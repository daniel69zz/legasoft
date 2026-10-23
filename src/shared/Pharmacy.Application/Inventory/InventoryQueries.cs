using Pharmacy.Application.Abstractions;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Inventory;

public sealed record StockRow(Guid ProductId, string Code, string Name, string Category, bool RequiresPrescription,
    decimal SalePrice, int Stock, int MinimumStock, string Location, DateOnly? NextExpiration, bool IsLowStock);

public sealed record InventoryOverview(IReadOnlyList<StockRow> Stock, IReadOnlyList<ExpiryAlert> ExpiryAlerts,
    IReadOnlyList<LowStockAlert> LowStockAlerts);

/// <summary>Caso de uso InventoryLocalService: stock vendible, lotes y alertas (RF-012, RF-013, RF-018, RF-022).</summary>
public sealed class InventoryQueries(ICatalogRepository catalog, IClock clock)
{
    public async Task<InventoryOverview> GetOverviewAsync(string? search = null, CancellationToken ct = default)
    {
        var today = clock.Today;
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

        return new InventoryOverview(rows, InventoryAlerts.Expiry(products, lots, today),
            InventoryAlerts.LowStock(products, lots, today));
    }
}
