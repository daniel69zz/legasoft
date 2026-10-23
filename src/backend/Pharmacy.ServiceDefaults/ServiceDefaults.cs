using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
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

    /// <summary>Serilog, ProblemDetails, health check y validación del JWT emitido por el Identity Service.</summary>
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
        return app;
    }

    /// <summary>
    /// Registra el DbContext sobre PostgreSQL (un usuario por servicio, ADR-005).
    /// Con una cadena "InMemory:nombre" usa el proveedor en memoria para pruebas.
    /// </summary>
    public static IServiceCollection AddPharmacyDb<TContext>(this IServiceCollection services,
        IConfiguration configuration, string name) where TContext : DbContext
    {
        var connection = configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Falta ConnectionStrings:{name}.");
        return services.AddDbContext<TContext>(o =>
        {
            if (connection.StartsWith("InMemory:", StringComparison.Ordinal))
                o.UseInMemoryDatabase(connection["InMemory:".Length..]);
            else
                o.UseNpgsql(connection).UseSnakeCaseNamingConvention();
        });
    }

    /// <summary>
    /// Crea las tablas del servicio si su schema aún no las tiene. Varios servicios
    /// comparten la instancia, por eso no se usa EnsureCreated. Las migraciones EF Core
    /// reemplazarán este paso cuando el modelo se estabilice.
    /// </summary>
    public static async Task EnsureSchemaAsync<TContext>(this IServiceProvider services, string probeTable,
        Func<TContext, Task>? seed = null) where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        if (db.Database.IsRelational())
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.Database.OpenConnectionAsync();
                    break;
                }
                catch (Exception) when (attempt < 10)
                {
                    Log.Warning("PostgreSQL no responde todavía (intento {Attempt}); reintentando", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT to_regclass('{probeTable}') IS NOT NULL";
            var exists = (bool)(await command.ExecuteScalarAsync())!;
            if (!exists)
                await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            await db.Database.CloseConnectionAsync();
        }
        if (seed is not null) await seed(db);
    }

    /// <summary>Protege los endpoints internos que solo invocan otros servicios (no pasan por el Gateway).</summary>
    public static bool HasInternalKey(this HttpContext context, IConfiguration configuration) =>
        context.Request.Headers[InternalKeyHeader] == configuration["Internal:Key"];
}
