using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Business.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Demo;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Identity;

namespace Pharmacy.IntegrationTests;

/// <summary>Consolidación de eventos en el Business Service (RF-104) y permisos de reportes.</summary>
public class BusinessApiTests : IDisposable
{
    private const string InternalKey = "clave-interna-solo-desarrollo";
    private readonly ServiceFactory<BusinessDb, BusinessDb> _factory = new();

    public void Dispose() => _factory.Dispose();

    private HttpClient InternalClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", InternalKey);
        return client;
    }

    private static SyncEnvelope SaleEvent(Guid lotId, int quantity)
    {
        var saleId = Guid.NewGuid();
        var payload = new SaleCreatedPayload(saleId, "PUESTO-01-000001", "PUESTO-01", "cajero1", Guid.NewGuid(),
            DateTimeOffset.Now, 5m, 0m, 5m, 10m, 5m, "", "", "",
            [new SaleLinePayload(DemoCatalog.ProductId(1), "Paracetamol", lotId, "PCM-2409", quantity, 0.5m)]);
        return new SyncEnvelope(Guid.NewGuid(), "PUESTO-01", SyncModules.Sales, EventTypes.SaleCreated, saleId, 1,
            DateTimeOffset.Now, JsonSerializer.Serialize(payload, OutboxMessage.Json));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("Requirement", "RF-104")]
    public async Task Los_endpoints_internos_exigen_la_clave_interna()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/internal/events",
            SaleEvent(Guid.NewGuid(), 1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("Requirement", "RF-104")]
    public async Task Una_venta_sincronizada_se_consolida_y_descuenta_el_stock_central()
    {
        var lotId = new Guid("00000000-0000-0000-0002-000000000002");
        var client = InternalClient();
        (await client.GetAsync("/health")).EnsureSuccessStatusCode();

        int before;
        using (var scope = _factory.Services.CreateScope())
            before = (await scope.ServiceProvider.GetRequiredService<BusinessDb>().Lots.FindAsync(lotId))!.Quantity;

        (await client.PostAsJsonAsync("/internal/events", SaleEvent(lotId, 10))).EnsureSuccessStatusCode();

        using var check = _factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<BusinessDb>();
        Assert.Equal(before - 10, (await db.Lots.FindAsync(lotId))!.Quantity);
        Assert.Equal(1, await db.Sales.CountAsync());
        Assert.Equal(1, await db.StockMovements.CountAsync());

        var dashboard = await _factory.ClientFor(Roles.GerenteGeneral).GetFromJsonAsync<DashboardSummary>("/api/dashboard");
        Assert.Equal(5m, dashboard!.SalesToday);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-35")]
    [Trait("Requirement", "RF-102")]
    public async Task Un_evento_desconocido_se_rechaza_de_forma_permanente()
    {
        var message = new SyncEnvelope(Guid.NewGuid(), "PUESTO-01", "sales", "Inventado", Guid.NewGuid(), 1,
            DateTimeOffset.Now, "{}");
        var response = await InternalClient().PostAsJsonAsync("/internal/events", message);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-32")]
    [Trait("Requirement", "RF-073")]
    public async Task El_cajero_no_ve_el_dashboard_gerencial()
    {
        var response = await _factory.ClientFor(Roles.Cajero).GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
