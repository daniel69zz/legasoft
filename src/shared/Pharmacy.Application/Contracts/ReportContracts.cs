namespace Pharmacy.Application.Contracts;

public sealed record SalesByCashierRow(DateOnly Day, string CashierName, int Sales, decimal Total);

public sealed record DashboardSummary(
    decimal SalesToday,
    int SalesCountToday,
    decimal SalesLast7Days,
    int LowStockProducts,
    int ExpiringLots,
    int ExpiredLots,
    int ClosuresWithDifference,
    IReadOnlyList<SalesByCashierRow> SalesByCashier);

public sealed record AlertDto(string Kind, string Product, string Detail);
