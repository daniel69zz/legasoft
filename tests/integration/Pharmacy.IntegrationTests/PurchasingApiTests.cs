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
    [Trait("Requirement", "RF-002")]
    public async Task Permisos_de_proveedores_y_compras_por_rol()
    {
        var cashier = _factory.ClientFor(Roles.Cajero);
        Assert.Equal(HttpStatusCode.Forbidden, (await cashier.GetAsync("/api/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _operations.PostAsJsonAsync("/api/suppliers", Supplier())).StatusCode);
    }
}
