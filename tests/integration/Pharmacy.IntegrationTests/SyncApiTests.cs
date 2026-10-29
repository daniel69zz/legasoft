using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;
using Sync.Api;

namespace Pharmacy.IntegrationTests;

/// <summary>Despachador falso: cuenta los envíos y puede simular que el Business Service está caído.</summary>
public sealed class FakeDispatcher() : EventDispatcher(new HttpClient(), null!)
{
    public List<Guid> Dispatched { get; } = [];
    public bool BusinessDown { get; set; }

    public override Task<DispatchResult> DispatchAsync(SyncEnvelope message, CancellationToken ct)
    {
        if (BusinessDown) return Task.FromResult(new DispatchResult(false, false, "caído"));
        Dispatched.Add(message.EventId);
        return Task.FromResult(new DispatchResult(true));
    }

    public override Task<PullResponse> GetCatalogChangesAsync(long since, CancellationToken ct) =>
        Task.FromResult(new PullResponse(since, []));
}

/// <summary>RF-081 (orden), RF-102 (idempotencia) y seguridad del Sync Service.</summary>
public class SyncApiTests : IDisposable
{
    private readonly FakeDispatcher _dispatcher = new();
    private readonly ServiceFactory<SyncDb, SyncDb> _factory;

    public SyncApiTests() =>
        _factory = new ServiceFactory<SyncDb, SyncDb>(s => s.AddSingleton<EventDispatcher>(_dispatcher));

    public void Dispose() => _factory.Dispose();

    private static SyncEnvelope Message(DateTimeOffset at, string device = "PUESTO-01") =>
        new(Guid.NewGuid(), device, SyncModules.Sales, EventTypes.SaleCreated, Guid.NewGuid(), 1, at, "{}");

    [Fact]
    [Trait("JiraIssue", "SCRUM-68")]
    [Trait("Requirement", "RF-102")]
    public async Task Un_evento_repetido_se_descarta_sin_duplicar()
    {
        var client = _factory.ClientFor(Roles.Cajero);
        var message = Message(DateTimeOffset.UtcNow);

        var first = await client.PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-01", [message]));
        var second = await client.PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-01", [message]));

        Assert.Equal(PushStatus.Accepted, (await first.Content.ReadFromJsonAsync<PushResponse>())!.Results[0].Status);
        Assert.Equal(PushStatus.Duplicate, (await second.Content.ReadFromJsonAsync<PushResponse>())!.Results[0].Status);
        Assert.Single(_dispatcher.Dispatched);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-68")]
    [Trait("Requirement", "RF-081")]
    public async Task Los_eventos_se_despachan_en_orden_cronologico()
    {
        var t = DateTimeOffset.UtcNow;
        var later = Message(t.AddMinutes(5));
        var earlier = Message(t);

        await _factory.ClientFor(Roles.Cajero).PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-01", [later, earlier]));

        Assert.Equal([earlier.EventId, later.EventId], _dispatcher.Dispatched);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-68")]
    [Trait("Requirement", "RF-080")]
    public async Task Si_el_dominio_no_responde_el_evento_queda_para_reintentar()
    {
        _dispatcher.BusinessDown = true;
        var response = await _factory.ClientFor(Roles.Cajero)
            .PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-01", [Message(DateTimeOffset.UtcNow)]));

        var result = (await response.Content.ReadFromJsonAsync<PushResponse>())!.Results.Single();
        Assert.Equal(PushStatus.Retry, result.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-68")]
    [Trait("Requirement", "RF-080")]
    public async Task Rechaza_lotes_de_otro_dispositivo_y_sin_token()
    {
        var spoofed = await _factory.ClientFor(Roles.Cajero, device: "PUESTO-01")
            .PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-02", [Message(DateTimeOffset.UtcNow, "PUESTO-02")]));
        var anonymous = await _factory.CreateClient()
            .PostAsJsonAsync("/sync/push", new PushRequest("PUESTO-01", []));

        Assert.Equal(HttpStatusCode.Forbidden, spoofed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
