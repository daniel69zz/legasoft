using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;

namespace Pharmacy.Desktop.Data;

/// <summary>LocalRepositories y OutboxRepository: implementan sobre SQLite las interfaces de Pharmacy.Application.</summary>
public sealed class LocalStore(LocalDb db) : ICatalogRepository, ISalesRepository, ICashRepository, IOutbox, IUnitOfWork
{
    public Task<List<Product>> GetProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default) => db.Products.ToListAsync(ct);

    public Task<List<Lot>> GetLotsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct = default) =>
        db.Lots.Where(l => productIds.Contains(l.ProductId)).ToListAsync(ct);

    public Task<List<Lot>> GetAllLotsAsync(CancellationToken ct = default) => db.Lots.ToListAsync(ct);

    public void AddMovement(StockMovement movement) => db.StockMovements.Add(movement);

    public void Add(Sale sale) => db.Sales.Add(sale);

    public Task<Sale?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Sales.SingleOrDefaultAsync(s => s.Id == id, ct);

    public Task<int> CountByDeviceAsync(string deviceId, CancellationToken ct = default) =>
        db.Sales.CountAsync(s => s.DeviceId == deviceId, ct);

    public void Add(CashSession session) => db.CashSessions.Add(session);

    public Task<CashSession?> GetOpenAsync(string deviceId, CancellationToken ct = default) =>
        db.CashSessions.SingleOrDefaultAsync(s => s.DeviceId == deviceId && s.Status == CashSessionStatus.Abierta, ct);

    public void Add(OutboxMessage message) => db.Outbox.Add(message);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
