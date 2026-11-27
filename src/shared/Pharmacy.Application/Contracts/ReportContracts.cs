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

/// <summary>SCRUM-123 · HU-06.4 (RF-019): movimiento de inventario consolidado en el servidor.</summary>
public sealed record InventoryMovementDto(DateTimeOffset OccurredAt, Guid ProductId, string ProductCode,
    string ProductName, string LotNumber, string Type, int Quantity, string Reason, string UserName);

/// <summary>
/// SCRUM-130 · HU-28.1 (RF-099): parámetros operativos. SCRUM-30 · HU-27 agrega el descuento máximo
/// (opcional para aceptar clientes anteriores al Sprint 3).
/// </summary>
public sealed record OperationalSettingsDto(int ExpiryThresholdDays, int AlertIntervalMinutes,
    int MaxDiscountPercent = Pharmacy.Domain.Sales.DiscountPolicy.DefaultMaxPercent);

/// <summary>SCRUM-33 · HU-28 (RF-097, RF-098): datos de la empresa y de la sucursal que salen en los comprobantes.</summary>
public sealed record CompanyProfileDto(string CompanyName, string CompanyTaxId, string CompanyAddress,
    string CompanyPhone, string BranchName, string BranchAddress)
{
    public static readonly CompanyProfileDto Default = new("Farmacia Pacheco", "", "La Paz, Bolivia", "",
        "Sopocachi", "La Paz");
}

/// <summary>SCRUM-32 · HU-19 (RF-074): agrupación del comparativo de ventas por cajero.</summary>
public static class SalesGrouping
{
    public const string Day = "dia";
    public const string Week = "semana";
    public const string Month = "mes";

    /// <summary>Primer día del período al que pertenece la fecha (semana de lunes a domingo).</summary>
    public static DateOnly PeriodStart(DateOnly day, string? groupBy) => groupBy switch
    {
        Week => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)),
        Month => new DateOnly(day.Year, day.Month, 1),
        _ => day,
    };
}

/// <summary>
/// SCRUM-31 · HU-18 (RF-068 a RF-072, RF-103): reporte tabular genérico. Se muestra en el Desktop y se
/// exporta a Excel o PDF con el mismo contenido.
/// </summary>
public sealed record ReportTableDto(string Title, string Subtitle, IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows, IReadOnlyList<string> Totals, string Notice = "");

public static class ReportKinds
{
    public const string Sales = "ventas";
    public const string Purchases = "compras";
    public const string Inventory = "inventario";
    public const string LowStock = "stock-bajo";
    public const string Expiring = "vencimientos";
    public const string DailyClosing = "cierre-diario";

    public static readonly IReadOnlyList<(string Kind, string Label)> All =
    [
        (Sales, "Ventas del período (RF-068)"),
        (Purchases, "Compras del período (RF-069)"),
        (Inventory, "Inventario por producto y lote (RF-070)"),
        (LowStock, "Productos con stock bajo (RF-071)"),
        (Expiring, "Medicamentos próximos a vencer (RF-072)"),
        (DailyClosing, "Cierre diario consolidado (RF-103)"),
    ];
}

/// <summary>SCRUM-28 · HU-24 (RF-084 a RF-087): cliente de la farmacia.</summary>
public sealed record CustomerDto(Guid Id, string Name, string TaxId, string Phone, string Email,
    DateTimeOffset CreatedAt);

public sealed record CustomerRequest(string Name, string TaxId, string Phone = "", string Email = "");

/// <summary>SCRUM-28 · HU-24 (RF-087): compra del cliente con sus productos.</summary>
public sealed record CustomerPurchaseDto(DateTimeOffset CreatedAt, string ReceiptNumber, string Status,
    decimal Total, IReadOnlyList<string> Products);

/// <summary>SCRUM-27 · HU-17 (RF-063 a RF-067, RF-101): factura simulada sin validez fiscal.</summary>
public sealed record InvoiceRequest(string CustomerName, string CustomerTaxId);

public sealed record InvoiceLineDto(string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

public sealed record InvoiceDto(int Number, Guid SaleId, string ReceiptNumber, DateTimeOffset IssuedAt,
    string IssuedBy, string CustomerName, string CustomerTaxId, IReadOnlyList<InvoiceLineDto> Lines,
    decimal Subtotal, decimal Discount, decimal Total, CompanyProfileDto Company)
{
    public const string Legend = "DOCUMENTO SIN VALIDEZ FISCAL · No válido para crédito fiscal";
}

/// <summary>SCRUM-36 · HU-23 (RF-082, RF-083): respaldo de la información del negocio.</summary>
public sealed record BackupDto(string Name, DateTimeOffset CreatedAt, string CreatedBy, string Kind, long SizeBytes,
    int Tables, long Rows);

public sealed record RestoreRequest(bool Confirm);

/// <summary>SCRUM-34 · HU-29 (RF-100): puesto asignado a una sucursal.</summary>
public sealed record BranchDeviceDto(string DeviceId, string Branch);

/// <summary>SCRUM-34 · HU-29: resumen de una sucursal (requiere conexión con el servidor).</summary>
public sealed record BranchSummaryDto(string Branch, IReadOnlyList<string> Devices, int Sales, decimal SalesTotal,
    int CashSessions, IReadOnlyList<BranchStockDto> StockMovements);

public sealed record BranchStockDto(string ProductCode, string ProductName, int NetQuantity);
