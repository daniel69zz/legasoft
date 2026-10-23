using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;

namespace Sync.Api;

/// <summary>
/// SyncController del Documento 3. Recibe lotes del Outbox en orden cronológico,
/// descarta duplicados (IdempotencyStore), despacha al Business Service
/// (EventDispatcher) y registra la versión por módulo y dispositivo (VersionManager).
/// </summary>
public static class SyncEndpoints
{
    public const int MaxBatchSize = 200;

    public static void MapSyncEndpoints(this WebApplication app)
    {
        var sync = app.MapGroup("/sync").RequireAuthorization();
        sync.MapPost("/push", PushAsync);
        sync.MapGet("/pull", PullAsync);
        sync.MapGet("/status/{deviceId}", async (string deviceId, SyncDb db) =>
            await db.ModuleVersions.Where(v => v.DeviceId == deviceId).ToListAsync());
    }

    private static async Task<IResult> PushAsync(PushRequest request, SyncDb db, EventDispatcher dispatcher,
        HttpContext http, TimeProvider time, ILogger<EventDispatcher> logger, CancellationToken ct)
    {
        if (request.Messages.Count > MaxBatchSize)
            return Results.Problem($"El lote supera los {MaxBatchSize} mensajes.", statusCode: 400);

        // El puesto del token debe coincidir con el que envía el lote.
        var tokenDevice = http.User.FindFirst("device")?.Value;
        if (tokenDevice != request.DeviceId)
            return Results.Problem("El dispositivo no coincide con la sesión.", statusCode: 403);

        var results = new List<PushItemResult>();
        foreach (var message in request.Messages.OrderBy(m => m.Timestamp))
        {
            if (message.DeviceId != request.DeviceId)
            {
                results.Add(new(message.EventId, PushStatus.Rejected, "Mensaje de otro dispositivo."));
                continue;
            }
            if (await db.ReceivedEvents.AnyAsync(e => e.EventId == message.EventId, ct))
            {
                results.Add(new(message.EventId, PushStatus.Duplicate));
                continue;
            }

            var dispatch = await dispatcher.DispatchAsync(message, ct);
            if (!dispatch.Success)
            {
                logger.LogWarning("Evento {EventId} ({EventType}) rechazado: {Error}", message.EventId,
                    message.EventType, dispatch.Error);
                results.Add(new(message.EventId, dispatch.Permanent ? PushStatus.Rejected : PushStatus.Retry, dispatch.Error));
                if (!dispatch.Permanent) break; // se conserva el orden: lo demás se reintenta después
                continue;
            }

            var now = time.GetUtcNow();
            db.ReceivedEvents.Add(new ReceivedEvent
            {
                EventId = message.EventId, DeviceId = message.DeviceId, Module = message.Module,
                EventType = message.EventType, EntityId = message.EntityId, Version = message.Version,
                Timestamp = message.Timestamp, ReceivedAt = now, Payload = message.Payload,
            });
            var version = await db.ModuleVersions.FindAsync([message.DeviceId, message.Module], ct);
            if (version is null)
                db.ModuleVersions.Add(version = new ModuleVersion { DeviceId = message.DeviceId, Module = message.Module });
            version.Version++;
            version.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            results.Add(new(message.EventId, PushStatus.Accepted));
        }
        return Results.Ok(new PushResponse(results));
    }

    /// <summary>Descarga incremental del catálogo y precios desde la versión conocida por el puesto.</summary>
    private static async Task<IResult> PullAsync(long since, EventDispatcher dispatcher, CancellationToken ct) =>
        Results.Ok(await dispatcher.GetCatalogChangesAsync(since, ct));
}
