using System.Net;
using System.Net.Http.Json;
using Business.Api;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>HU-05: CP-RF-009 a CP-RF-012 sobre el catálogo del Business Service.</summary>
public class CatalogApiTests : IDisposable
{
    private readonly ServiceFactory<BusinessDb, BusinessDb> _factory = new();

    public void Dispose() => _factory.Dispose();

    private static SaveProductRequest Ibuprofeno(string code = "MED-001") => new(code, "Ibuprofeno", "Ibuprofeno",
        "400 mg", "Tableta", "Inti", "Analgésicos", false, 0.80m, 20, "Estante B1");

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-009")]
    [Trait("Requirement", "RF-012")]
    public async Task El_farmaceutico_registra_un_medicamento_y_lo_encuentra_al_buscar()
    {
        var client = _factory.ClientFor(Roles.Farmaceutico, "farmaceutico");

        var created = await client.PostAsJsonAsync("/api/catalog", Ibuprofeno());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await created.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.IsActive);

        foreach (var term in new[] { "MED-001", "ibupro", "Analgésicos" })
        {
            var found = await client.GetFromJsonAsync<List<ProductDto>>($"/api/catalog?search={term}");
            Assert.Contains(found!, p => p.Id == product.Id);
        }
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-009")]
    public async Task Rechaza_un_codigo_de_producto_duplicado()
    {
        var client = _factory.ClientFor(Roles.Farmaceutico);
        (await client.PostAsJsonAsync("/api/catalog", Ibuprofeno())).EnsureSuccessStatusCode();

        var duplicate = await client.PostAsJsonAsync("/api/catalog", Ibuprofeno("med-001"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("MED-001", await duplicate.Content.ReadAsStringAsync());
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-010")]
    public async Task Modificar_un_medicamento_lo_publica_para_los_puestos()
    {
        var client = _factory.ClientFor(Roles.Farmaceutico);
        var product = await (await client.PostAsJsonAsync("/api/catalog", Ibuprofeno())).Content
            .ReadFromJsonAsync<ProductDto>();

        var updated = await client.PutAsJsonAsync($"/api/catalog/{product!.Id}",
            Ibuprofeno() with { Name = "Ibuprofeno Forte", SalePrice = 1.10m, RequiresPrescription = true });
        updated.EnsureSuccessStatusCode();

        var pulled = await CatalogForDevicesAsync();
        var synced = pulled.Products.Single(p => p.Id == product.Id);
        Assert.Equal("Ibuprofeno Forte", synced.Name);
        Assert.Equal(1.10m, synced.SalePrice);
        Assert.True(synced.RequiresPrescription);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-011")]
    public async Task Un_medicamento_desactivado_llega_inactivo_a_los_puestos_y_queda_auditado()
    {
        var client = _factory.ClientFor(Roles.Farmaceutico, "farmaceutico");
        var product = await (await client.PostAsJsonAsync("/api/catalog", Ibuprofeno())).Content
            .ReadFromJsonAsync<ProductDto>();

        (await client.PostAsync($"/api/catalog/{product!.Id}/deactivate", null)).EnsureSuccessStatusCode();

        var pulled = await CatalogForDevicesAsync();
        Assert.False(pulled.Products.Single(p => p.Id == product.Id).IsActive);
        var audit = await _factory.ClientFor(Roles.Administrador).GetFromJsonAsync<List<AuditEntryDto>>("/api/audit");
        Assert.Contains(audit!, a => a.Action == "Desactivación de medicamento" && a.UserName == "farmaceutico");
    }

    [Theory]
    [InlineData(Roles.Cajero)]
    [InlineData(Roles.Almacen)]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-002")]
    public async Task Los_roles_sin_permiso_de_catalogo_reciben_403(string role)
    {
        var response = await _factory.ClientFor(role).PostAsJsonAsync("/api/catalog", Ibuprofeno());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Lo que descarga el Sync Service para los puestos (endpoint interno).</summary>
    private async Task<PullResponse> CatalogForDevicesAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", "clave-interna-solo-desarrollo");
        return (await client.GetFromJsonAsync<PullResponse>("/internal/catalog?since=0"))!;
    }
}
