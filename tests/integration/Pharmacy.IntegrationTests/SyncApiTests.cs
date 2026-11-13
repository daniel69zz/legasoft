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

    public List<LotChangeDto> Lots { get; } = [];

    public bool? LastSnapshot { get; private set; }

    public override Task<LotPullResponse> GetLotChangesAsync(long since, bool snapshot, CancellationToken ct)
    {
        LastSnapshot = snapshot;
        var lots = Lots.Where(l => snapshot || l.Sequence > since).ToList();
        return Task.FromResult(new LotPullResponse(lots.Count == 0 ? since : lots.Max(l => l.Sequence), lots));
    }
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
    [Trait("JiraIssue", "SCRUM-112")]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-033")]
    public async Task Los_puestos_descargan_solo_los_lotes_nuevos_desde_su_version()
    {
        var product = Guid.NewGuid();
        var expiration = new DateOnly(2027, 12, 31);
        _dispatcher.Lots.Add(new LotChangeDto(1, Guid.NewGuid(), product, "PCM-2511", expiration, 100));
        _dispatcher.Lots.Add(new LotChangeDto(2, Guid.NewGuid(), product, "PCM-2512", expiration, 90));
        var client = _factory.ClientFor(Roles.Cajero);

        var first = await client.GetFromJsonAsync<LotPullResponse>("/sync/pull-lots?since=0");
        var next = await client.GetFromJsonAsync<LotPullResponse>($"/sync/pull-lots?since={first!.Version}");
        var anonymous = await _factory.CreateClient().GetAsync("/sync/pull-lots?since=0");

        Assert.Equal((2L, 2), (first.Version, first.Lots.Count));
        Assert.Empty(next!.Lots);
        Assert.False(_dispatcher.LastSnapshot);

        // La primera descarga de un puesto pide la foto completa (SCRUM-112).
        var all = await client.GetFromJsonAsync<LotPullResponse>($"/sync/pull-lots?since={first.Version}&snapshot=true");
        Assert.True(_dispatcher.LastSnapshot);
        Assert.Equal(2, all!.Lots.Count);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
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
