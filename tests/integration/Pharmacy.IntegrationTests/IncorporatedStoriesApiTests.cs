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
    [Trait("JiraIssue", "SCRUM-124")]
    [Trait("JiraIssue", "SCRUM-125")]
    [Trait("Requirement", "RF-034")]
    [Trait("Requirement", "RF-035")]
    [Trait("Requirement", "RF-036")]
    public async Task Precios_por_proveedor_con_historial_y_comparacion_ordenada()
    {
        await PriceAsync(DemoPurchasing.DistribuidoraAndina, 0.30m);
        await PriceAsync(DemoPurchasing.DistribuidoraAndina, 0.28m);
        await PriceAsync(DemoPurchasing.DrogueriaIllimani, 0.25m);

        var current = await _operations.GetFromJsonAsync<List<SupplierPriceDto>>(
            $"/api/suppliers/{DemoPurchasing.DistribuidoraAndina}/prices");
        var history = await _operations.GetFromJsonAsync<List<SupplierPriceDto>>(
            $"/api/suppliers/{DemoPurchasing.DistribuidoraAndina}/prices/history?productId={Paracetamol}");
        var compared = await _operations.GetFromJsonAsync<List<SupplierPriceDto>>(
            $"/api/products/{Paracetamol}/supplier-prices");

        Assert.Equal(0.28m, Assert.Single(current!).Price);
        Assert.Equal([0.28m, 0.30m], history!.Select(h => h.Price));
        Assert.Equal([(DemoPurchasing.DrogueriaIllimani, 0.25m), (DemoPurchasing.DistribuidoraAndina, 0.28m)],
            compared!.Select(c => (c.SupplierId, c.Price)));

        // Un proveedor desactivado no aparece en la comparación.
        (await _admin.PostAsync($"/api/suppliers/{DemoPurchasing.DrogueriaIllimani}/deactivate", null))
            .EnsureSuccessStatusCode();
        compared = await _operations.GetFromJsonAsync<List<SupplierPriceDto>>(
            $"/api/products/{Paracetamol}/supplier-prices");
        Assert.Equal(DemoPurchasing.DistribuidoraAndina, Assert.Single(compared!).SupplierId);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-126")]
    [Trait("Requirement", "RF-061")]
    [Trait("Requirement", "RF-062")]
    public async Task La_entrega_de_caja_se_consolida_con_su_confirmacion_y_auditoria()
    {
        var session = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow;
        await SendAsync(EventTypes.CashSessionOpened, session,
            new CashSessionOpenedPayload(session, "PUESTO-01", "cajero1", 450m, at.AddHours(-8)));
        await SendAsync(EventTypes.CashSessionClosed, session, new CashSessionClosedPayload(session, "PUESTO-01",
            "cajero1", 450m, 0m, 0m, 450m, 450m, 0m, "", at.AddHours(-8), at.AddMinutes(-5),
            new Dictionary<decimal, int> { [200m] = 2, [50m] = 1 }));
        await SendAsync(EventTypes.CashHandoverRequested, session,
            new CashHandoverPayload(session, "PUESTO-01", "cajero1", "cajero2", 450m, null, false, at.AddMinutes(-4)));
        await SendAsync(EventTypes.CashHandoverConfirmed, session,
            new CashHandoverPayload(session, "PUESTO-01", "cajero1", "cajero2", 450m, 450m, true, at));

        var audit = await _admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit?action=entrega");
        Assert.Contains(audit!, a => a.Action == "Entrega de caja" && a.UserName == "cajero1");
        Assert.Contains(audit!, a => a.Action == "Confirmación de entrega de caja" && a.UserName == "cajero2"
                                     && a.DeviceId == "PUESTO-01");
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-128")]
    [Trait("Requirement", "RF-075")]
    public async Task La_auditoria_filtra_por_usuario_y_operacion_y_el_cajero_recibe_403()
    {
        await SendAsync(EventTypes.StockMovementCreated, Guid.NewGuid(), new StockMovementPayload(Guid.NewGuid(),
            Paracetamol, ParacetamolLot, "PCM-2401", "Ajuste", -1, "Rotura", "almacen", DateTimeOffset.UtcNow));
        await PriceAsync(DemoPurchasing.DistribuidoraAndina, 0.30m);

        var filtered = await _factory.ClientFor(Roles.GerenteGeneral).GetFromJsonAsync<List<AuditEntryDto>>(
            "/api/audit?user=almacen&action=movimiento%20de%20inventario");

        var entry = Assert.Single(filtered!);
        Assert.Equal(("almacen", "PUESTO-01"), (entry.UserName, entry.DeviceId));
        Assert.Equal(HttpStatusCode.Forbidden, (await _factory.ClientFor(Roles.Cajero).GetAsync("/api/audit")).StatusCode);
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

    private async Task PriceAsync(Guid supplier, decimal price) =>
        (await _operations.PostAsJsonAsync($"/api/suppliers/{supplier}/prices",
            new RegisterSupplierPriceRequest(Paracetamol, price))).EnsureSuccessStatusCode();

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
