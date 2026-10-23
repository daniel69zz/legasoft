using System.Text.Json;
using Pharmacy.Application.Contracts;

namespace Pharmacy.Application.Sync;

public enum OutboxStatus
{
    Pendiente,
    Sincronizado,
    Rechazado,
}

/// <summary>
/// Mensaje pendiente de sincronizar. Se guarda en la misma transacción que la
/// operación de negocio y el Sync Worker lo envía cuando hay conexión (ADR-003).
/// </summary>
public class OutboxMessage
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private OutboxMessage() { }

    public static OutboxMessage Create<TPayload>(string deviceId, string module, string eventType, Guid entityId,
        TPayload payload, DateTimeOffset now, int version = 1) => new()
    {
        EventId = Guid.NewGuid(),
        DeviceId = deviceId,
        Module = module,
        EventType = eventType,
        EntityId = entityId,
        Version = version,
        Timestamp = now,
        Payload = JsonSerializer.Serialize(payload, Json),
        Status = OutboxStatus.Pendiente,
    };

    public Guid EventId { get; private set; }
    public string DeviceId { get; private set; } = "";
    public string Module { get; private set; } = "";
    public string EventType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string Payload { get; private set; } = "";
    public OutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string LastError { get; private set; } = "";
    public DateTimeOffset? SyncedAt { get; private set; }

    public SyncEnvelope ToEnvelope() =>
        new(EventId, DeviceId, Module, EventType, EntityId, Version, Timestamp, Payload);

    public void MarkSynced(DateTimeOffset now)
    {
        Status = OutboxStatus.Sincronizado;
        SyncedAt = now;
        LastError = "";
    }

    public void MarkFailed(string error, bool permanent)
    {
        Attempts++;
        LastError = error;
        if (permanent) Status = OutboxStatus.Rechazado;
    }
}
