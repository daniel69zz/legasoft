namespace Pharmacy.Application.Contracts;

public static class SyncModules
{
    public const string Sales = "sales";
    public const string Inventory = "inventory";
    public const string Cash = "cash";
}

public static class EventTypes
{
    public const string SaleCreated = "SaleCreated";
    public const string SaleVoided = "SaleVoided";
    public const string StockMovementCreated = "StockMovementCreated";
    public const string CashSessionOpened = "CashSessionOpened";
    public const string CashMovementRegistered = "CashMovementRegistered";
    public const string CashSessionClosed = "CashSessionClosed";
}

/// <summary>Mensaje Outbox tal como viaja al Sync Service (Documento 3, sección 7.2).</summary>
public sealed record SyncEnvelope(
    Guid EventId,
    string DeviceId,
    string Module,
    string EventType,
    Guid EntityId,
    int Version,
    DateTimeOffset Timestamp,
    string Payload);

public sealed record PushRequest(string DeviceId, IReadOnlyList<SyncEnvelope> Messages);

public static class PushStatus
{
    public const string Accepted = "accepted";
    public const string Duplicate = "duplicate";
    public const string Rejected = "rejected";
    public const string Retry = "retry";
}

public sealed record PushItemResult(Guid EventId, string Status, string? Error = null);

public sealed record PushResponse(IReadOnlyList<PushItemResult> Results);

public sealed record CatalogProductDto(Guid Id, string Code, string Name, string ActiveIngredient,
    string Concentration, string Presentation, string Laboratory, string Category, bool RequiresPrescription,
    decimal SalePrice, int MinimumStock, string Location, bool IsActive, DateTimeOffset UpdatedAt);

/// <summary>Cambios de catálogo y precios desde una versión (descarga incremental).</summary>
public sealed record PullResponse(long Version, IReadOnlyList<CatalogProductDto> Products);
