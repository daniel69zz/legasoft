using System.Net;
using System.Net.Security;
using System.Security.Authentication;

namespace Pharmacy.Desktop.Services;

/// <summary>
/// SCRUM-100 · HU-T02.2 (RNF-003): fuera de la propia computadora, el puesto solo habla con el Gateway por
/// HTTPS con TLS 1.2 o superior y con un certificado válido. HTTP sin cifrar solo se acepta
/// contra localhost (backend de desarrollo con docker compose).
/// </summary>
public static class GatewayUrlPolicy
{
    public const SslProtocols AllowedProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

    public static Uri Validate(string gatewayUrl)
    {
        if (!Uri.TryCreate(gatewayUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"Desktop:GatewayUrl no es una URL válida: {gatewayUrl}");
        if (uri.Scheme == Uri.UriSchemeHttp && !IsLocal(uri))
            throw new InvalidOperationException(
                $"El Gateway {uri.Host} debe usarse con https:// (RNF-003). HTTP solo se permite con localhost.");
        return uri;
    }

    /// <summary>Handler con TLS 1.2+ y validación de certificado (no se desactiva en ningún entorno).</summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        SslOptions = new SslClientAuthenticationOptions { EnabledSslProtocols = AllowedProtocols },
    };

    private static bool IsLocal(Uri uri) =>
        uri.IsLoopback || (IPAddress.TryParse(uri.Host, out var ip) && IPAddress.IsLoopback(ip));
}
