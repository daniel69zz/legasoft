using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Pharmacy.Domain.Common;
using Serilog;

namespace Pharmacy.ServiceDefaults;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "legasoft-farmacia";
    public string Audience { get; set; } = "pharmacy-desktop";

    /// <summary>Clave simétrica compartida por los servicios. En producción va en un secreto, no en appsettings.</summary>
    public string Key { get; set; } = "";

    public int ExpirationMinutes { get; set; } = 480;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}

public static class ServiceDefaults
{
    public const string CorrelationHeader = "X-Correlation-Id";
    public const string InternalKeyHeader = "X-Internal-Key";

    /// <summary>
    /// Serilog, ProblemDetails, health check, documento OpenAPI y validación del JWT emitido
    /// por el Identity Service.
    /// </summary>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, config) => config
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", context.HostingEnvironment.ApplicationName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {Message:lj}{NewLine}{Exception}"));

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks();
        builder.Services.AddOpenApiDocument(builder.Environment.ApplicationName);

        var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (jwt.Key.Length < 32)
            throw new InvalidOperationException("Configure Jwt:Key con al menos 32 caracteres.");
        builder.Services.AddSingleton(jwt);
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.SigningKey,
                    RoleClaimType = "role",
                    NameClaimType = "name",
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
        builder.Services.AddAuthorization();
        return builder;
    }

    /// <summary>Middleware de correlación, excepciones como ProblemDetails, autenticación y /health.</summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var id = context.Request.Headers[CorrelationHeader].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
            context.Response.Headers[CorrelationHeader] = id;
            using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
                await next();
        });
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            var (status, title) = error switch
            {
                DomainException => (StatusCodes.Status422UnprocessableEntity, error.Message),
                FluentValidation.ValidationException v => (StatusCodes.Status400BadRequest,
                    string.Join(" ", v.Errors.Select(e => e.ErrorMessage))),
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servicio."),
            };
            context.Response.StatusCode = status;
            await Results.Problem(title: title, statusCode: status).ExecuteAsync(context);
        }));
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        // Solo el JSON: la interfaz Swagger UI está en el Gateway y reúne los tres servicios.
        app.UseSwagger();
        return app;
    }

    /// <summary>OpenAPI del servicio con autenticación Bearer. Los endpoints /internal no se documentan.</summary>
    private static void AddOpenApiDocument(this IServiceCollection services, string serviceName)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = serviceName,
                Version = "v1",
                Description = "Sistema de Gestión para Farmacéutica (Legasoft). Se consume a través del API Gateway.",
            });
            o.DocInclusionPredicate((_, api) => api.RelativePath?.StartsWith("internal") != true);
            o.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));
            var bearer = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token obtenido con POST /auth/login.",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            };
            o.AddSecurityDefinition("Bearer", bearer);
            o.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = [] });
        });
    }

    /// <summary>
    /// Registra el DbContext sobre PostgreSQL (un usuario por servicio, ADR-005). El historial de
    /// migraciones va en un schema propio del servicio, porque nadie puede crear objetos en public.
    /// Con una cadena "InMemory:nombre" usa el proveedor en memoria para pruebas.
    /// </summary>
    public static IServiceCollection AddPharmacyDb<TContext>(this IServiceCollection services,
        IConfiguration configuration, string name, string historySchema) where TContext : DbContext
    {
        var connection = configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Falta ConnectionStrings:{name}.");
        return services.AddDbContext<TContext>(o =>
        {
            if (connection.StartsWith("InMemory:", StringComparison.Ordinal))
                o.UseInMemoryDatabase(connection["InMemory:".Length..]);
            else
                o.UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", historySchema))
                    .UseSnakeCaseNamingConvention();
        });
    }

    /// <summary>
    /// Aplica las migraciones EF Core pendientes del servicio (carpeta Migrations) y luego la
    /// semilla. Espera a PostgreSQL porque en Docker el servicio puede arrancar antes que la base.
    /// </summary>
    public static async Task MigrateAsync<TContext>(this IServiceProvider services, Func<TContext, Task>? seed = null)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        if (db.Database.IsRelational())
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Npgsql.NpgsqlException) when (attempt < 10)
                {
                    Log.Warning("PostgreSQL no responde todavía (intento {Attempt}); reintentando", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }
        if (seed is not null) await seed(db);
    }

    /// <summary>Protege los endpoints internos que solo invocan otros servicios (no pasan por el Gateway).</summary>
    public static bool HasInternalKey(this HttpContext context, IConfiguration configuration) =>
        context.Request.Headers[InternalKeyHeader] == configuration["Internal:Key"];
}
