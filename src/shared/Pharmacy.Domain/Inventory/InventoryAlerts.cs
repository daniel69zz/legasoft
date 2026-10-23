using Pharmacy.Domain.Catalog;

namespace Pharmacy.Domain.Inventory;

public enum ExpiryStatus
{
    Vigente,
    PorVencer,
    Vencido,
}

public sealed record ExpiryAlert(Guid ProductId, string ProductName, string LotNumber, DateOnly ExpirationDate,
    int Quantity, int DaysToExpire, ExpiryStatus Status)
{
    public string Description => Status == ExpiryStatus.Vencido
        ? $"Vencido hace {-DaysToExpire} días"
        : $"Vence en {DaysToExpire} días";
}

public sealed record LowStockAlert(Guid ProductId, string ProductName, int Stock, int MinimumStock);

/// <summary>Alertas de vencimiento (RF-022, RF-023) y de stock mínimo (RF-018).</summary>
public static class InventoryAlerts
{
    public const int DefaultExpiryThresholdDays = 60;

    public static ExpiryStatus StatusOf(Lot lot, DateOnly today, int thresholdDays = DefaultExpiryThresholdDays) =>
        lot.IsExpired(today) ? ExpiryStatus.Vencido
        : lot.DaysToExpire(today) <= thresholdDays ? ExpiryStatus.PorVencer
        : ExpiryStatus.Vigente;

    public static IReadOnlyList<ExpiryAlert> Expiry(IEnumerable<Product> products, IEnumerable<Lot> lots,
        DateOnly today, int thresholdDays = DefaultExpiryThresholdDays)
    {
        var names = products.ToDictionary(p => p.Id, p => p.DisplayName);
        return lots
            .Where(l => l.Quantity > 0)
            .Select(l => new { Lot = l, Status = StatusOf(l, today, thresholdDays) })
            .Where(x => x.Status != ExpiryStatus.Vigente)
            .OrderBy(x => x.Lot.ExpirationDate)
            .Select(x => new ExpiryAlert(x.Lot.ProductId, names.GetValueOrDefault(x.Lot.ProductId, "?"),
                x.Lot.LotNumber, x.Lot.ExpirationDate, x.Lot.Quantity, x.Lot.DaysToExpire(today), x.Status))
            .ToList();
    }

    public static IReadOnlyList<LowStockAlert> LowStock(IEnumerable<Product> products, IEnumerable<Lot> lots,
        DateOnly today)
    {
        var stock = lots.GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => FefoAllocator.SellableStock(g, today));
        return products
            .Where(p => p.IsActive)
            .Select(p => new LowStockAlert(p.Id, p.DisplayName, stock.GetValueOrDefault(p.Id), p.MinimumStock))
            .Where(a => a.Stock <= a.MinimumStock)
            .OrderBy(a => a.Stock)
            .ToList();
    }
}
