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
