using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;
using Pharmacy.Application.Sync;

namespace Pharmacy.Application.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }
    DateOnly Today => DateOnly.FromDateTime(Now.LocalDateTime);
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}

/// <summary>Usuario y puesto que ejecutan la operación (RF-047).</summary>
public interface ICurrentUser
{
    string UserName { get; }
    string Role { get; }
    string DeviceId { get; }
}

public interface ICatalogRepository
{
    Task<List<Product>> GetProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default);
    Task<List<Lot>> GetLotsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default);
    Task<List<Lot>> GetAllLotsAsync(CancellationToken ct = default);
    void AddMovement(StockMovement movement);

    /// <summary>Historial de movimientos de un producto, del más reciente al más antiguo.</summary>
    Task<List<StockMovement>> GetMovementsAsync(Guid productId, int take = 50, CancellationToken ct = default);
}

public interface ISalesRepository
{
    void Add(Sale sale);
    Task<Sale?> GetAsync(Guid id, CancellationToken ct = default);
    Task<int> CountByDeviceAsync(string deviceId, CancellationToken ct = default);
}

public interface ICashRepository
{
    void Add(CashSession session);
    Task<CashSession?> GetOpenAsync(string deviceId, CancellationToken ct = default);

    /// <summary>Último turno del puesto (abierto, cerrado o con entrega pendiente).</summary>
    Task<CashSession?> GetLatestAsync(string deviceId, CancellationToken ct = default);
}

/// <summary>
/// SCRUM-130 · HU-28.1 (RF-099): parámetros operativos que el administrador configura en el servidor y que
/// los puestos reciben con la sincronización.
/// </summary>
public interface IOperationalSettings
{
    Task<int> GetExpiryThresholdDaysAsync(CancellationToken ct = default);
}

public interface IOutbox
{
    void Add(OutboxMessage message);
}

/// <summary>
/// Confirma en una sola transacción todos los cambios de la operación,
/// incluidos los mensajes de Outbox (ADR-003, ADR-007).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
