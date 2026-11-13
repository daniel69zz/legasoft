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
    [Trait("JiraIssue", "SCRUM-123")]
    [Trait("Requirement", "RF-019")]
    public async Task El_historial_general_filtra_por_tipo_y_el_cajero_no_lo_ve()
    {
        await SendAsync(EventTypes.StockMovementCreated, Guid.NewGuid(), new StockMovementPayload(Guid.NewGuid(),
            Paracetamol, ParacetamolLot, "PCM-2401", "Ajuste", -3, "Producto dañado", "almacen", DateTimeOffset.UtcNow));
        await SendAsync(EventTypes.StockMovementCreated, Guid.NewGuid(), new StockMovementPayload(Guid.NewGuid(),
            Paracetamol, ParacetamolLot, "PCM-2401", "Entrada", 5, "Devolución", "almacen", DateTimeOffset.UtcNow));

        var adjustments = await _operations.GetFromJsonAsync<List<InventoryMovementDto>>(
            "/api/inventory/movements?type=Ajuste");
        var all = await _operations.GetFromJsonAsync<List<InventoryMovementDto>>("/api/inventory/movements");

        var row = Assert.Single(adjustments!);
        Assert.Equal(("P-0001", "PCM-2401", -3, "Producto dañado", "almacen"),
            (row.ProductCode, row.LotNumber, row.Quantity, row.Reason, row.UserName));
        Assert.Equal(2, all!.Count);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _factory.ClientFor(Roles.Cajero).GetAsync("/api/inventory/movements")).StatusCode);
    }

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

    private async Task SendAsync<T>(string eventType, Guid entityId, T payload)
    {
        var module = eventType.StartsWith("Cash") ? SyncModules.Cash
            : eventType.StartsWith("Sale") ? SyncModules.Sales : SyncModules.Inventory;
        var message = new SyncEnvelope(Guid.NewGuid(), "PUESTO-01", module, eventType, entityId, 1,
            DateTimeOffset.UtcNow, JsonSerializer.Serialize(payload, OutboxMessage.Json));
        var response = await Internal().PostAsJsonAsync("/internal/events", message);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    private HttpClient Internal()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "clave-interna-solo-desarrollo");
        return client;
    }
}
