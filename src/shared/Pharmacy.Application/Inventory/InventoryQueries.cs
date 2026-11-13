using Pharmacy.Application.Abstractions;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Inventory;

public sealed record StockRow(Guid ProductId, string Code, string Name, string Category, bool RequiresPrescription,
    decimal SalePrice, int Stock, int MinimumStock, string Location, DateOnly? NextExpiration, bool IsLowStock);

public sealed record InventoryOverview(IReadOnlyList<StockRow> Stock, IReadOnlyList<ExpiryAlert> ExpiryAlerts,
    IReadOnlyList<LowStockAlert> LowStockAlerts, int ExpiryThresholdDays = InventoryAlerts.DefaultExpiryThresholdDays);

/// <summary>SCRUM-122 · HU-07.3 (RF-021): existencia de un lote con su vencimiento y estado.</summary>
public sealed record LotStockRow(Guid LotId, string LotNumber, DateOnly ExpirationDate, int Quantity, int DaysToExpire,
    ExpiryStatus Status)
{
    public string StatusLabel => Status switch
    {
        ExpiryStatus.Vencido => "Vencido",
        ExpiryStatus.PorVencer => "Por vencer",
        _ => "Vigente",
    };

    public bool IsExpired => Status == ExpiryStatus.Vencido;
    public bool IsExpiring => Status == ExpiryStatus.PorVencer;
}

/// <summary>Caso de uso InventoryLocalService: stock vendible, lotes y alertas (RF-012, RF-013, RF-018, RF-022).</summary>
public sealed class InventoryQueries(ICatalogRepository catalog, IClock clock, IOperationalSettings settings)
{
    public async Task<InventoryOverview> GetOverviewAsync(string? search = null, CancellationToken ct = default)
    {
        var today = clock.Today;
        var threshold = await settings.GetExpiryThresholdDaysAsync(ct);
        var products = await catalog.GetAllProductsAsync(ct);
        var lots = await catalog.GetAllLotsAsync(ct);
        var lotsByProduct = lots.ToLookup(l => l.ProductId);

        var rows = products
            .Where(p => p.IsActive && p.Matches(search ?? ""))
            .OrderBy(p => p.Name)
            .Select(p =>
            {
                var productLots = lotsByProduct[p.Id].ToList();
                var stock = FefoAllocator.SellableStock(productLots, today);
                var next = productLots.Where(l => l.Quantity > 0 && !l.IsExpired(today))
                    .Select(l => (DateOnly?)l.ExpirationDate).Min();
                return new StockRow(p.Id, p.Code, p.DisplayName, p.Category, p.RequiresPrescription, p.SalePrice,
                    stock, p.MinimumStock, p.Location, next, stock <= p.MinimumStock);
            })
            .ToList();

        return new InventoryOverview(rows, InventoryAlerts.Expiry(products, lots, today, threshold),
            InventoryAlerts.LowStock(products, lots, today), threshold);
    }

    /// <summary>SCRUM-122 · HU-07.3 (RF-021): lotes con existencia de un producto, con su estado según el umbral vigente.</summary>
    public async Task<IReadOnlyList<LotStockRow>> GetLotDetailsAsync(Guid productId, CancellationToken ct = default)
    {
        var today = clock.Today;
        var threshold = await settings.GetExpiryThresholdDaysAsync(ct);
        return (await GetLotsAsync(productId, ct))
            .Select(l => new LotStockRow(l.Id, l.LotNumber, l.ExpirationDate, l.Quantity, l.DaysToExpire(today),
                InventoryAlerts.StatusOf(l, today, threshold)))
            .ToList();
    }

    /// <summary>RF-021: lotes con existencia de un producto, el que vence antes primero.</summary>
    public async Task<IReadOnlyList<Lot>> GetLotsAsync(Guid productId, CancellationToken ct = default) =>
        (await catalog.GetLotsAsync([productId], ct))
        .Where(l => l.Quantity > 0)
        .OrderBy(l => l.ExpirationDate)
        .ToList();

    /// <summary>SCRUM-101 · HU-06.3: historial de movimientos con tipo, lote, cantidad, motivo, usuario, fecha y hora.</summary>
    public async Task<IReadOnlyList<StockMovementRow>> GetMovementsAsync(Guid productId, CancellationToken ct = default)
    {
        var lots = (await catalog.GetLotsAsync([productId], ct)).ToDictionary(l => l.Id, l => l.LotNumber);
        return (await catalog.GetMovementsAsync(productId, ct: ct))
            .Select(m => new StockMovementRow(m.OccurredAt, m.Type.ToString(), lots.GetValueOrDefault(m.LotId, "?"),
                m.Quantity, m.Reason, m.UserName))
            .ToList();
    }
}
