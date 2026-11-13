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

/// <summary>HU-06.4 (RF-019): movimiento de inventario consolidado en el servidor.</summary>
public sealed record InventoryMovementDto(DateTimeOffset OccurredAt, Guid ProductId, string ProductCode,
    string ProductName, string LotNumber, string Type, int Quantity, string Reason, string UserName);

/// <summary>HU-28.1 (RF-099): parámetros operativos de inventario.</summary>
public sealed record OperationalSettingsDto(int ExpiryThresholdDays, int AlertIntervalMinutes);
