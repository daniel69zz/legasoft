using System.Net;
using System.Net.Http.Json;
using Identity.Api;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>CP-RF-001 a CP-RF-006 y RNF-004 sobre el Identity Service.</summary>
public class IdentityApiTests : IDisposable
{
    private const string Password = "Farmacia2026!";
    private readonly ServiceFactory<IdentityDb, IdentityDb> _factory = new();

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> LoginAsync(string user, string password, string device = "PUESTO-01") =>
        _factory.CreateClient().PostAsJsonAsync("/auth/login", new LoginRequest(user, password, device));

    [Fact]
    public async Task Login_valido_devuelve_token_con_rol()
    {
        var response = await LoginAsync("admin", Password);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal(Roles.Administrador, body!.Role);
        Assert.False(string.IsNullOrEmpty(body.Token));
    }

    [Fact]
    public async Task Credenciales_invalidas_dan_un_mensaje_generico()
    {
        var wrongPassword = await LoginAsync("admin", "otra-clave");
        var unknownUser = await LoginAsync("noexiste", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownUser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cajero_en_equipo_no_autorizado_es_rechazado()
    {
        var response = await LoginAsync("cajero1", Password, device: "LAPTOP-PERSONAL");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cinco_intentos_fallidos_bloquean_la_cuenta()
    {
        for (var i = 0; i < AuthEndpoints.MaxFailedAttempts; i++) await LoginAsync("cajero2", "mala");

        var response = await LoginAsync("cajero2", Password);
        Assert.Equal(HttpStatusCode.Locked, response.StatusCode);
    }

    [Fact]
    public async Task Cajero_no_puede_gestionar_usuarios()
    {
        var response = await _factory.ClientFor(Roles.Cajero).GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrador_registra_desactiva_y_queda_auditado()
    {
        await LoginAsync("admin", Password); // fuerza la siembra
        var admin = _factory.ClientFor(Roles.Administrador, "admin");

        var created = await admin.PostAsJsonAsync("/api/users",
            new CreateUserRequest("cajero3", "Cajero de prueba", "ClaveSegura1", Roles.Cajero));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var duplicate = await admin.PostAsJsonAsync("/api/users",
            new CreateUserRequest("cajero3", "Otro", "ClaveSegura1", Roles.Cajero));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var user = await created.Content.ReadFromJsonAsync<UserDto>();
        (await admin.PostAsync($"/api/users/{user!.Id}/deactivate", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("cajero3", "ClaveSegura1")).StatusCode);

        var audit = await admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/users/audit");
        Assert.Contains(audit!, a => a.Action == "Alta de usuario");
        Assert.Contains(audit!, a => a.Action == "Desactivación de usuario");
    }
}
