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
    // SCRUM-37 · HU-T01: el cupo es por puesto y usuario (claims del token), no por IP. Detrás de nginx y
    // Cloudflare todas las solicitudes llegan desde 127.0.0.1 y compartían un único cupo entre todas las cajas.
    o.AddPolicy("por-puesto", context => RateLimitPartition.GetFixedWindowLimiter(
        RateLimitKey.For(context),
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

/// <summary>
/// Clave del límite de solicitudes: «puesto/usuario» leídos del JWT o, sin token, la IP. El Gateway no valida
/// la firma (lo hacen los servicios); un token alterado solo cambia el cupo, no da acceso a nada.
/// </summary>
static class RateLimitKey
{
    public static string For(HttpContext context)
    {
        var auth = context.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = auth[7..].Split('.');
            if (parts.Length == 3)
            {
                try
                {
                    var json = System.Text.Encoding.UTF8.GetString(Base64Url(parts[1]));
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var device = doc.RootElement.TryGetProperty("device", out var d) ? d.GetString() : null;
                    var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (device is not null || name is not null) return $"{device}/{name}";
                }
                catch (Exception e) when (e is FormatException or System.Text.Json.JsonException
                                              or InvalidOperationException)
                {
                    // token ilegible: se usa la IP
                }
            }
        }
        return context.Connection.RemoteIpAddress?.ToString() ?? "desconocido";
    }

    private static byte[] Base64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}
