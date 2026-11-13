using System.Net;
using System.Net.Http.Json;
using Identity.Api;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>HU-03.2 (CP-RF-007): el administrador solo inicia sesión en equipos autorizados para su perfil.</summary>
public class AdministratorDeviceTests : IDisposable
{
    private const string Password = "Farmacia2026!";
    private readonly ServiceFactory<IdentityDb, IdentityDb> _factory = new();

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> LoginAsync(string user, string device) =>
        _factory.CreateClient().PostAsJsonAsync("/auth/login", new LoginRequest(user, Password, device));

    [Theory]
    [InlineData("PUESTO-01", HttpStatusCode.OK)]
    [InlineData("PUESTO-02", HttpStatusCode.Forbidden)]   // autorizado para cajeros, no para el administrador
    [InlineData("PUESTO-09", HttpStatusCode.Forbidden)]   // no registrado
    [Trait("JiraIssue", "SCRUM-127")]
    [Trait("Requirement", "RF-007")]
    public async Task El_administrador_solo_entra_desde_equipos_autorizados_para_su_perfil(string device,
        HttpStatusCode expected)
    {
        var response = await LoginAsync("admin", device);
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-127")]
    [Trait("Requirement", "RF-007")]
    public async Task El_intento_rechazado_queda_en_auditoria_y_no_afecta_a_otros_roles()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await LoginAsync("admin", "PUESTO-09")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("cajero1", "PUESTO-02")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("gerente", "PUESTO-09")).StatusCode);

        var audit = await _factory.ClientFor(Roles.Administrador, "admin")
            .GetFromJsonAsync<List<AuditEntryDto>>("/api/users/audit");
        Assert.Contains(audit!, a => a.Action == "Acceso rechazado" && a.Detail.Contains("PUESTO-09"));
    }
}
