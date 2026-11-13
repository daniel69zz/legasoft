using System.Security.Authentication;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.UiTests;

/// <summary>HU-T02.2 (CP-RNF-003): el puesto solo usa HTTP contra localhost y exige TLS 1.2 o superior.</summary>
public class GatewayUrlPolicyTests
{
    [Theory]
    [InlineData("http://localhost:5100")]
    [InlineData("http://127.0.0.1:5100")]
    [InlineData("https://farmacia.example.com")]
    [Trait("JiraIssue", "SCRUM-100")]
    [Trait("Requirement", "RNF-003")]
    public void Acepta_https_y_http_solo_en_la_propia_computadora(string url)
    {
        Assert.Equal(new Uri(url), GatewayUrlPolicy.Validate(url));
    }

    [Theory]
    [InlineData("http://farmacia.example.com")]
    [InlineData("http://192.168.1.20:5100")]
    [InlineData("ftp://farmacia.example.com")]
    [Trait("JiraIssue", "SCRUM-100")]
    [Trait("Requirement", "RNF-003")]
    public void Rechaza_un_gateway_remoto_sin_cifrar(string url)
    {
        Assert.Throws<InvalidOperationException>(() => GatewayUrlPolicy.Validate(url));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-100")]
    [Trait("Requirement", "RNF-003")]
    public void El_cliente_HTTP_solo_negocia_TLS_12_o_13_y_valida_el_certificado()
    {
        using var handler = GatewayUrlPolicy.CreateHandler();

        Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, handler.SslOptions.EnabledSslProtocols);
        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
    }
}
