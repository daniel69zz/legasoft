using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Desktop.Data;
using Pharmacy.Domain.Catalog;

namespace Pharmacy.Desktop.Services;

/// <summary>
/// SyncWorker (Documento 3, C3 Desktop): con conexión, descarga los cambios de
/// catálogo y envía por lotes los mensajes pendientes del Outbox en orden
/// cronológico. Sin conexión reintenta con retroceso exponencial.
/// </summary>
public sealed class SyncWorker(
    IServiceScopeFactory scopes,
    ApiClient api,
    Session session,
    SyncStatus status,
    DesktopOptions options,
    IServiceProvider services,
    ILogger<SyncWorker> logger) : BackgroundService
{
    public const int BatchSize = 100;
    private const string CatalogVersionKey = "inventory_version";
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _wake = new(0, 1);

    public void Wake()
    {
        if (_wake.CurrentCount == 0) _wake.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.SyncIntervalSeconds);
        var delay = interval;
        while (!stoppingToken.IsCancellationRequested)
        {
            var ok = await SyncOnceAsync(stoppingToken);
            delay = ok ? interval : TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDelay.Ticks));
            try
            {
                await _wake.WaitAsync(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async Task<bool> SyncOnceAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LocalDb>();
            if (!session.IsSignedIn)
            {
                status.Update(pending: await PendingAsync(db, ct));
                return true;
            }
            if (session.Token is null)
                await services.GetRequiredService<AuthService>().TryReconnectAsync();

            await PullCatalogAsync(db, ct);
            await PushOutboxAsync(db, ct);
            status.Update(online: true, pending: await PendingAsync(db, ct), lastSync: DateTimeOffset.Now, error: "");
            return true;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            await UpdatePendingAsync(false, "Sin conexión con el servidor", ct);
            return false;
        }
        catch (ApiException e)
        {
            if (e.Status == 401) session.SetToken(null);
            logger.LogWarning("Sincronización rechazada: {Error}", e.Message);
            await UpdatePendingAsync(true, e.Message, ct);
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PullCatalogAsync(LocalDb db, CancellationToken ct)
    {
        var state = await db.SyncState.FindAsync([CatalogVersionKey], ct);
        var since = long.TryParse(state?.Value, out var v) ? v : 0;
        var changes = await api.PullAsync(since, ct);
        if (changes.Products.Count == 0) return;

        var ids = changes.Products.Select(p => p.Id).ToList();
        var local = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        foreach (var p in changes.Products)
        {
            if (local.TryGetValue(p.Id, out var product))
            {
                product.ApplyCatalogChange(p.Name, p.ActiveIngredient, p.Concentration, p.Presentation, p.Laboratory,
                    p.Category, p.RequiresPrescription, p.SalePrice, p.MinimumStock, p.Location, p.IsActive,
                    p.UpdatedAt);
            }
            else
            {
                var added = new Product(p.Id, p.Code, p.Name, p.ActiveIngredient, p.Concentration, p.Presentation,
                    p.Laboratory, p.Category, p.RequiresPrescription, p.SalePrice, p.MinimumStock, p.Location);
                added.SetActive(p.IsActive, p.UpdatedAt);
                db.Products.Add(added);
            }
        }
        if (state is null) db.SyncState.Add(state = new SyncState { Key = CatalogVersionKey });
        state.Value = changes.Version.ToString();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Catálogo actualizado: {Count} productos", changes.Products.Count);
    }

    private async Task PushOutboxAsync(LocalDb db, CancellationToken ct)
    {
        while (true)
        {
            var batch = (await db.Outbox.Where(m => m.Status == OutboxStatus.Pendiente).ToListAsync(ct))
                .OrderBy(m => m.Timestamp).Take(BatchSize).ToList();
            if (batch.Count == 0) return;

            var response = await api.PushAsync(
                new PushRequest(session.DeviceId, batch.Select(m => m.ToEnvelope()).ToList()), ct);
            var byId = batch.ToDictionary(m => m.EventId);
            var retry = false;
            foreach (var r in response.Results)
            {
                var message = byId[r.EventId];
                switch (r.Status)
                {
                    case PushStatus.Accepted or PushStatus.Duplicate:
                        message.MarkSynced(DateTimeOffset.Now);
                        break;
                    case PushStatus.Rejected:
                        message.MarkFailed(r.Error ?? "Rechazado", permanent: true);
                        break;
                    default:
                        message.MarkFailed(r.Error ?? "Reintentar", permanent: false);
                        retry = true;
                        break;
                }
            }
            await db.SaveChangesAsync(ct);
            if (retry || response.Results.Count < batch.Count) return;
        }
    }

    private async Task UpdatePendingAsync(bool online, string error, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDb>();
        status.Update(online: online, pending: await PendingAsync(db, ct), error: error);
    }

    private static Task<int> PendingAsync(LocalDb db, CancellationToken ct) =>
        db.Outbox.CountAsync(m => m.Status == OutboxStatus.Pendiente, ct);
}
