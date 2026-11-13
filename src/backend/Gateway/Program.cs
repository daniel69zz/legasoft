using System.Security.Authentication;
using System.Threading.RateLimiting;
using Serilog;

// API Gateway (Documento 3, C2): enruta a los servicios, limita solicitudes y
// propaga el identificador de correlación. Los endpoints /internal no se exponen.
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] Gateway {Message:lj}{NewLine}{Exception}"));

// SCRUM-100 · HU-T02.2 (RNF-003): si el Gateway publica HTTPS directamente, solo acepta TLS 1.2 o superior. En el VPS
// el TLS lo termina el nginx del servidor (deploy/vps/nginx-farmacia.conf) con la misma restricción.
builder.WebHost.ConfigureKestrel(k => k.ConfigureHttpsDefaults(h =>
    h.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13));

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("por-puesto", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-Id";
    if (!context.Request.Headers.ContainsKey(header))
        context.Request.Headers[header] = Guid.NewGuid().ToString("N");
    context.Response.Headers[header] = context.Request.Headers[header].ToString();
    await next();
});
app.UseRateLimiter();
app.MapHealthChecks("/health");

// Swagger UI con los documentos OpenAPI de cada servicio, que llegan por las rutas /docs/*.
app.UseSwaggerUI(o =>
{
    o.RoutePrefix = "swagger";
    o.DocumentTitle = "API · Sistema de Gestión para Farmacéutica";
    o.SwaggerEndpoint("/docs/identity/swagger.json", "Identity Service (login y usuarios)");
    o.SwaggerEndpoint("/docs/business/swagger.json", "Business Service (catálogo, inventario, reportes)");
    o.SwaggerEndpoint("/docs/sync/swagger.json", "Sync Service (sincronización de puestos)");
});
app.MapReverseProxy();
app.Run();
