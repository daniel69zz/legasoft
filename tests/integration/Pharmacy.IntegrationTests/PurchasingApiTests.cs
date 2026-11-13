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
/// HU-08 (CP-RF-026 a CP-RF-029), HU-09 (CP-RF-030 a CP-RF-033, CP-RF-107, CP-RF-108) y la
/// consolidación en el servidor de los ajustes de HU-06.3 sobre el Business Service.
/// </summary>
public class PurchasingApiTests : IDisposable
{
    private static readonly Guid Paracetamol = DemoCatalog.ProductId(1);
    private static readonly Guid ParacetamolLot = new("00000000-0000-0000-0002-000000000001");

    private readonly ServiceFactory<BusinessDb, BusinessDb> _factory = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _operations;

    public PurchasingApiTests()
    {
        _admin = _factory.ClientFor(Roles.Administrador, "admin");
        _operations = _factory.ClientFor(Roles.GerenteOperaciones, "operaciones");
    }

    public void Dispose() => _factory.Dispose();

    private static SaveSupplierRequest Supplier(string taxId = "4455667788", string phone = "2-2223344") =>
        new(taxId, "Laboratorios del Sur", "Ana Choque", phone, "ventas@sur.example", "El Alto");

    private static DateOnly Expiration => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(400);

    [Fact]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-026")]
    public async Task El_administrador_registra_un_proveedor_y_queda_disponible_para_compras()
    {
        var created = await _admin.PostAsJsonAsync("/api/suppliers", Supplier());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var supplier = (await created.Content.ReadFromJsonAsync<SupplierDto>())!;

        var forPurchases = await _operations.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers?search=sur");
        Assert.Contains(forPurchases!, s => s.Id == supplier.Id && s.IsActive);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-026")]
    public async Task Rechaza_datos_obligatorios_incompletos_indicando_el_faltante_y_NIT_duplicado()
    {
        var incomplete = await _admin.PostAsJsonAsync("/api/suppliers", Supplier(phone: ""));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
        Assert.Contains("teléfono", await incomplete.Content.ReadAsStringAsync());

        (await _admin.PostAsJsonAsync("/api/suppliers", Supplier())).EnsureSuccessStatusCode();
        var duplicate = await _admin.PostAsJsonAsync("/api/suppliers", Supplier());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-027")]
    public async Task Modificar_un_proveedor_queda_auditado()
    {
        var supplier = (await (await _admin.PostAsJsonAsync("/api/suppliers", Supplier())).Content
            .ReadFromJsonAsync<SupplierDto>())!;

        var updated = await _admin.PutAsJsonAsync($"/api/suppliers/{supplier.Id}",
            Supplier() with { Name = "Laboratorios del Sur S.A.", TaxId = "otro" });

        var dto = (await updated.Content.ReadFromJsonAsync<SupplierDto>())!;
        Assert.Equal(("Laboratorios del Sur S.A.", "4455667788"), (dto.Name, dto.TaxId));
        var audit = await _admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.Contains(audit!, a => a.Action == "Modificación de proveedor" && a.UserName == "admin");
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-030")]
    [Trait("Requirement", "RF-031")]
    [Trait("Requirement", "RF-032")]
    [Trait("Requirement", "RF-033")]
    public async Task Recepcion_completa_de_100_unidades_aumenta_el_stock_en_100()
    {
        var stockBefore = await StockAsync(Paracetamol);
        var purchase = await CreatePurchaseAsync(100, 0.30m);
        Assert.Equal(30.00m, purchase.Total);
        Assert.Equal("Pendiente", purchase.Status);

        var received = await ReceiveAsync(purchase, "PCM-2511", 100, 0);

        Assert.Equal("Recibida", received.Status);
        Assert.Equal(stockBefore + 100, await StockAsync(Paracetamol));
        var lots = await LotsForDevicesAsync(0);
        Assert.Contains(lots.Lots, l => l.LotNumber == "PCM-2511" && l.Quantity == 100 && l.ProductId == Paracetamol);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-107")]
    [Trait("Requirement", "RF-108")]
    public async Task Con_90_en_buen_estado_y_10_danadas_solo_ingresan_90()
    {
        var stockBefore = await StockAsync(Paracetamol);
        var purchase = await CreatePurchaseAsync(100, 0.30m);

        var received = await ReceiveAsync(purchase, "PCM-2512", 90, 10);

        var line = received.Lines.Single();
        Assert.Equal((90, 10, 10, 0), (line.ReceivedQuantity, line.DamagedQuantity, line.Difference, line.MissingQuantity));
        Assert.Equal(stockBefore + 90, await StockAsync(Paracetamol));
        var audit = await _admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.Contains(audit!, a => a.Action == "Recepción de compra" && a.Detail.Contains("dañadas 10"));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-032")]
    public async Task Una_recepcion_invalida_no_cambia_el_stock_ni_la_compra()
    {
        var stockBefore = await StockAsync(Paracetamol);
        var purchase = await CreatePurchaseAsync(100, 0.30m);

        var response = await _operations.PostAsJsonAsync($"/api/purchases/{purchase.Id}/receive",
            new ReceivePurchaseRequest([new(purchase.Lines[0].Id, "", Expiration, 100, 0)]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(stockBefore, await StockAsync(Paracetamol));
        var reloaded = await _operations.GetFromJsonAsync<PurchaseDto>($"/api/purchases/{purchase.Id}");
        Assert.Equal("Pendiente", reloaded!.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-028")]
    [Trait("Requirement", "RF-029")]
    public async Task El_proveedor_muestra_sus_productos_y_su_historial_de_compras()
    {
        var purchase = await CreatePurchaseAsync(100, 0.30m);
        await ReceiveAsync(purchase, "PCM-2513", 100, 0);

        var products = await _admin.GetFromJsonAsync<List<SupplierProductDto>>(
            $"/api/suppliers/{DemoPurchasing.DrogueriaIllimani}/products");
        var history = await _admin.GetFromJsonAsync<List<PurchaseDto>>(
            $"/api/suppliers/{DemoPurchasing.DrogueriaIllimani}/purchases");

        var product = Assert.Single(products!);
        Assert.Equal((Paracetamol, 100, 0.30m), (product.ProductId, product.TotalQuantity, product.LastUnitCost));
        var entry = Assert.Single(history!);
        Assert.Equal((purchase.Number, 30.00m), (entry.Number, entry.Total));
        Assert.Single(entry.Lines);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-002")]
    public async Task Permisos_de_proveedores_y_compras_por_rol()
    {
        var cashier = _factory.ClientFor(Roles.Cajero);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/purchases")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _operations.PostAsJsonAsync("/api/suppliers", Supplier())).StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    public async Task Un_ajuste_del_puesto_se_aplica_una_sola_vez_en_el_lote_central()
    {
        var stockBefore = await StockAsync(Paracetamol);
        var payload = new StockMovementPayload(Guid.NewGuid(), Paracetamol, ParacetamolLot, "PCM-2401", "Ajuste", -3,
            "Producto dañado", "almacen", DateTimeOffset.UtcNow);
        var message = new SyncEnvelope(Guid.NewGuid(), "PUESTO-01", SyncModules.Inventory,
            EventTypes.StockMovementCreated, payload.MovementId, 1, DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(payload, OutboxMessage.Json));

        var client = Internal();
        (await client.PostAsJsonAsync("/internal/events", message)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/internal/events", message)).EnsureSuccessStatusCode();

        Assert.Equal(stockBefore - 3, await StockAsync(Paracetamol));
        var audit = await _admin.GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.Single(audit!, a => a.Action == "Movimiento de inventario" && a.Detail.Contains("Producto dañado"));
    }

    private async Task<PurchaseDto> CreatePurchaseAsync(int quantity, decimal unitCost)
    {
        var response = await _operations.PostAsJsonAsync("/api/purchases", new CreatePurchaseRequest(
            DemoPurchasing.DrogueriaIllimani, [new PurchaseItemRequest(Paracetamol, quantity, unitCost)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseDto>())!;
    }

    private async Task<PurchaseDto> ReceiveAsync(PurchaseDto purchase, string lot, int good, int damaged)
    {
        var response = await _operations.PostAsJsonAsync($"/api/purchases/{purchase.Id}/receive",
            new ReceivePurchaseRequest([new(purchase.Lines[0].Id, lot, Expiration, good, damaged)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PurchaseDto>())!;
    }

    private async Task<int> StockAsync(Guid productId)
    {
        var products = await _admin.GetFromJsonAsync<List<JsonElement>>("/api/products");
        return products!.Single(p => p.GetProperty("id").GetGuid() == productId).GetProperty("stock").GetInt32();
    }

    /// <summary>Lo que descarga el Sync Service para los puestos (endpoint interno).</summary>
    private async Task<LotPullResponse> LotsForDevicesAsync(long since) =>
        (await Internal().GetFromJsonAsync<LotPullResponse>($"/internal/lots?since={since}"))!;

    private HttpClient Internal()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "clave-interna-solo-desarrollo");
        return client;
    }
}
