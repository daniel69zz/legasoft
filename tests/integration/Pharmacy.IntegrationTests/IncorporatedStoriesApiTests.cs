using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Business.Api;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Demo;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>
/// Historias incorporadas al Sprint 2 sobre el Business Service: movimientos (SCRUM-123), precios
/// (SCRUM-124, 125), entrega de caja (SCRUM-126), auditoría (SCRUM-128), exportación (SCRUM-129) y
/// parámetros (SCRUM-130).
/// </summary>
public class IncorporatedStoriesApiTests : IDisposable
{
    private static readonly Guid Paracetamol = DemoCatalog.ProductId(1);
    private static readonly Guid ParacetamolLot = new("00000000-0000-0000-0002-000000000001");

    private readonly ServiceFactory<BusinessDb, BusinessDb> _factory = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _operations;

    public IncorporatedStoriesApiTests()
    {
        _admin = _factory.ClientFor(Roles.Administrador, "admin");
        _operations = _factory.ClientFor(Roles.GerenteOperaciones, "operaciones");
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    [Trait("JiraIssue", "SCRUM-130")]
    [Trait("Requirement", "RF-099")]
    public async Task El_umbral_configurado_llega_a_los_puestos_y_cambia_las_alertas()
    {
        var before = await _factory.ClientFor(Roles.GerenteGeneral).GetFromJsonAsync<DashboardSummary>("/api/dashboard");

        var invalid = await _admin.PutAsJsonAsync("/api/settings", new OperationalSettingsDto(0, 60));
        var forbidden = await _operations.PutAsJsonAsync("/api/settings", new OperationalSettingsDto(90, 60));
        (await _admin.PutAsJsonAsync("/api/settings", new OperationalSettingsDto(90, 30))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(new OperationalSettingsDto(90, 30), await _operations.GetFromJsonAsync<OperationalSettingsDto>("/api/settings"));
        var pulled = await Internal().GetFromJsonAsync<PullResponse>("/internal/catalog?since=0");
        Assert.Equal(90, pulled!.ExpiryThresholdDays);
        // El suero oral (vence en 90 días) pasa a «por vencer» con el umbral de 90.
        var after = await _factory.ClientFor(Roles.GerenteGeneral).GetFromJsonAsync<DashboardSummary>("/api/dashboard");
        Assert.True(after!.ExpiringLots > before!.ExpiringLots);
        var worker = await Internal().GetFromJsonAsync<OperationalSettingsDto>("/internal/settings");
        Assert.Equal(30, worker!.AlertIntervalMinutes);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-130")]
    [Trait("Requirement", "RF-099")]
    public async Task Sin_configurar_rige_la_frecuencia_del_despliegue()
    {
        var worker = await Internal().GetFromJsonAsync<OperationalSettingsDto>("/internal/settings");
        Assert.Equal((60, 0), (worker!.ExpiryThresholdDays, worker.AlertIntervalMinutes));
    }

    private HttpClient Internal()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "clave-interna-solo-desarrollo");
        return client;
    }
}
