using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Desktop.Data;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;

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
    private const string LotsVersionKey = "lots_version";

    // Marca que el puesto ya alineó sus lotes con los del servidor (SCRUM-112).
    private const string LotsSnapshotKey = "lots_snapshot_at";
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
            await PullLotsAsync(db, ct);
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
        await SaveThresholdAsync(db, changes.ExpiryThresholdDays, ct);
        if (changes.Products.Count == 0) return;

        var ids = changes.Products.Select(p => p.Id).ToList();
        var local = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var addedCount = 0;
        var details = new List<string>(changes.Products.Count);
        foreach (var p in changes.Products)
        {
            if (local.TryGetValue(p.Id, out var product))
            {
                product.ApplyCatalogChange(p.Name, p.ActiveIngredient, p.Concentration, p.Presentation, p.Laboratory,
                    p.Category, p.RequiresPrescription, p.SalePrice, p.MinimumStock, p.Location, p.IsActive,
                    p.UpdatedAt);
                details.Add($"Actualizado: {p.Code} · {p.Name}");
            }
            else
            {
                var newProduct = new Product(p.Id, p.Code, p.Name, p.ActiveIngredient, p.Concentration, p.Presentation,
                    p.Laboratory, p.Category, p.RequiresPrescription, p.SalePrice, p.MinimumStock, p.Location);
                newProduct.SetActive(p.IsActive, p.UpdatedAt);
                db.Products.Add(newProduct);
                details.Add($"Nuevo: {p.Code} · {p.Name}");
                addedCount++;
            }
        }
        db.SyncDownloads.Add(new SyncDownload
        {
            OccurredAt = DateTimeOffset.Now,
            Module = "Productos",
            ItemCount = changes.Products.Count,
            Summary = $"{changes.Products.Count} productos recibidos ({addedCount} nuevos, {changes.Products.Count - addedCount} actualizados)",
            Details = string.Join(Environment.NewLine, details),
        });
        if (state is null) db.SyncState.Add(state = new SyncState { Key = CatalogVersionKey });
        state.Value = changes.Version.ToString();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Catálogo actualizado: {Count} productos", changes.Products.Count);
    }

    /// <summary>HU-28.1 (RF-099): guarda el umbral de vencimiento configurado en el servidor.</summary>
    private static async Task SaveThresholdAsync(LocalDb db, int? days, CancellationToken ct)
    {
        if (days is null) return;
        var state = await db.SyncState.FindAsync([LocalStore.ExpiryThresholdKey], ct);
        if (state?.Value == days.ToString()) return;
        if (state is null) db.SyncState.Add(state = new SyncState { Key = LocalStore.ExpiryThresholdKey });
        state.Value = days.Value.ToString();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// HU-09 y SCRUM-112: descarga los lotes del servidor para venderlos con FEFO en el puesto. La
    /// primera vez pide todos (snapshot) y alinea número, vencimiento y, si el puesto no los movió,
    /// el stock de los que ya tiene; después solo recibe los lotes nuevos de las compras.
    /// </summary>
    private async Task PullLotsAsync(LocalDb db, CancellationToken ct)
    {
        var state = await db.SyncState.FindAsync([LotsVersionKey], ct);
        var snapshotDone = await db.SyncState.FindAsync([LotsSnapshotKey], ct);
        var since = long.TryParse(state?.Value, out var v) ? v : 0;
        var snapshot = snapshotDone is null;
        var changes = await api.PullLotsAsync(since, snapshot, ct);

        if (changes.Lots.Count > 0)
        {
            var ids = changes.Lots.Select(l => l.LotId).ToList();
            var local = await db.Lots.Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
            var moved = (await db.StockMovements.Where(m => ids.Contains(m.LotId)).Select(m => m.LotId)
                .Distinct().ToListAsync(ct)).ToHashSet();
            var result = LotSync.Merge(local, moved, changes.Lots);
            db.Lots.AddRange(result.Added);
            db.SyncDownloads.Add(new SyncDownload
            {
                OccurredAt = DateTimeOffset.Now,
                Module = "Lotes",
                ItemCount = changes.Lots.Count,
                Summary = $"{changes.Lots.Count} lotes recibidos ({result.Added.Count} nuevos, {result.Updated} alineados)",
                Details = string.Join(Environment.NewLine, changes.Lots.Select(l =>
                    $"{l.LotNumber} · vence {l.ExpirationDate:dd/MM/yyyy} · {l.Quantity} u.")),
            });
            logger.LogInformation("Lotes del servidor: {Added} nuevos y {Updated} alineados", result.Added.Count,
                result.Updated);
        }

        if (state is null) db.SyncState.Add(state = new SyncState { Key = LotsVersionKey });
        state.Value = changes.Version.ToString();
        if (snapshot) db.SyncState.Add(new SyncState { Key = LotsSnapshotKey, Value = DateTimeOffset.Now.ToString("O") });
        await db.SaveChangesAsync(ct);
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
