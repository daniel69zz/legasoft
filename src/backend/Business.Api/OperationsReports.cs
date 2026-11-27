using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Business.Api;

/// <summary>
/// SCRUM-31 · HU-18 (RF-068 a RF-072, RF-103, RF-104): reportes operativos del período con los datos
/// consolidados en el servidor. Cada reporte es una tabla que el Desktop muestra y exporta a Excel o PDF.
/// </summary>
public static class OperationsReports
{
    private static readonly CultureInfo Bo = CultureInfo.GetCultureInfo("es-BO");

    /// <summary>Rango en hora de La Paz. RF-068: una fecha inicial posterior a la final se rechaza.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End, DateOnly From, DateOnly To) Range(DateOnly? from,
        DateOnly? to, TimeProvider time)
    {
        var last = to ?? BusinessEndpoints.Today(time);
        var first = from ?? last.AddDays(-6);
        if (first > last)
            throw new DomainException(
                $"La fecha inicial ({first:dd/MM/yyyy}) es posterior a la fecha final ({last:dd/MM/yyyy}).");
        return (Local(first), Local(last.AddDays(1)), first, last);
    }

    public static async Task<ReportTableDto> BuildAsync(BusinessDb db, string kind, DateOnly? from, DateOnly? to,
        string? branch, TimeProvider time) => kind switch
    {
        ReportKinds.Sales => await SalesAsync(db, Range(from, to, time), branch),
        ReportKinds.Purchases => await PurchasesAsync(db, Range(from, to, time)),
        ReportKinds.Inventory => await InventoryAsync(db, time),
        ReportKinds.LowStock => await LowStockAsync(db, time),
        ReportKinds.Expiring => await ExpiringAsync(db, time),
        ReportKinds.DailyClosing => await DailyClosingAsync(db, to ?? from ?? BusinessEndpoints.Today(time), branch),
        _ => throw new DomainException($"Reporte desconocido: {kind}."),
    };

    /// <summary>RF-068: ventas del período con su total.</summary>
    private static async Task<ReportTableDto> SalesAsync(BusinessDb db,
        (DateTimeOffset Start, DateTimeOffset End, DateOnly From, DateOnly To) r, string? branch)
    {
        var sales = (await db.Sales.Where(s => s.CreatedAt >= r.Start && s.CreatedAt < r.End).ToListAsync())
            .Where(s => string.IsNullOrWhiteSpace(branch) || s.Branch == branch)
            .OrderBy(s => s.CreatedAt).ToList();
        var completed = sales.Where(s => s.Status == "Completada").ToList();
        return new ReportTableDto("Reporte de ventas", Period(r.From, r.To, branch),
            ["Fecha", "Recibo", "Cajero", "Puesto", "Sucursal", "Cliente", "Receta", "Estado", "Subtotal", "Descuento", "Total"],
            sales.Select(s => Cells(When(s.CreatedAt), s.ReceiptNumber, s.CashierName, s.DeviceId, s.Branch,
                s.CustomerName, s.PrescriptionNumber, s.Status, Money(s.Subtotal), Money(s.Discount), Money(s.Total))).ToList(),
            completed.Count == 0 ? [] : Cells($"{completed.Count} completadas", "", "", "", "", "", "", "",
                Money(completed.Sum(s => s.Subtotal)), Money(completed.Sum(s => s.Discount)), Money(completed.Sum(s => s.Total))),
            sales.Count == 0 ? "No hay ventas en el período." : "");
    }

    /// <summary>RF-069: compras del período con proveedor, productos, cantidades y monto total.</summary>
    private static async Task<ReportTableDto> PurchasesAsync(BusinessDb db,
        (DateTimeOffset Start, DateTimeOffset End, DateOnly From, DateOnly To) r)
    {
        var purchases = (await db.Purchases.Where(p => p.CreatedAt >= r.Start && p.CreatedAt < r.End).ToListAsync())
            .OrderBy(p => p.CreatedAt).ToList();
        var rows = purchases.SelectMany(p => p.Lines.OrderBy(l => l.Position).Select(l => Cells(When(p.CreatedAt),
            p.Number, p.SupplierName, l.ProductName, l.Quantity.ToString(Bo), Money(l.UnitCost), Money(l.Subtotal),
            p.Status.ToString()))).ToList();
        return new ReportTableDto("Reporte de compras", Period(r.From, r.To, null),
            ["Fecha", "Compra", "Proveedor", "Producto", "Cantidad", "Costo unit.", "Subtotal", "Estado"], rows,
            purchases.Count == 0 ? [] : Cells($"{purchases.Count} compras", "", "", "",
                purchases.Sum(p => p.Lines.Sum(l => l.Quantity)).ToString(Bo), "", Money(purchases.Sum(p => p.Total)), ""),
            purchases.Count == 0 ? "No hay compras en el período." : "");
    }

    /// <summary>RF-070: por producto, el stock actual, el stock por lote y el nivel mínimo.</summary>
    private static async Task<ReportTableDto> InventoryAsync(BusinessDb db, TimeProvider time)
    {
        var today = BusinessEndpoints.Today(time);
        var products = await db.Products.OrderBy(p => p.Name).ToListAsync();
        var lots = (await db.Lots.ToListAsync()).ToLookup(l => l.ProductId);
        var rows = new List<IReadOnlyList<string>>();
        foreach (var p in products)
        {
            var sellable = FefoAllocator.SellableStock(lots[p.Id], today);
            var productLots = lots[p.Id].Where(l => l.Quantity > 0).OrderBy(l => l.ExpirationDate).ToList();
            if (productLots.Count == 0)
                rows.Add(Cells(p.Code, p.Name, p.MinimumStock.ToString(Bo), sellable.ToString(Bo), "—", "", "0", "Sin stock"));
            foreach (var l in productLots)
                rows.Add(Cells(p.Code, p.Name, p.MinimumStock.ToString(Bo), sellable.ToString(Bo), l.LotNumber,
                    l.ExpirationDate.ToString("dd/MM/yyyy"), l.Quantity.ToString(Bo),
                    l.IsExpired(today) ? "Vencido" : "Vigente"));
        }
        return new ReportTableDto("Reporte de inventario", $"Al {today:dd/MM/yyyy}",
            ["Código", "Producto", "Mínimo", "Stock vendible", "Lote", "Vencimiento", "Cantidad", "Estado"], rows,
            Cells($"{products.Count} productos", "", "", products.Sum(p => FefoAllocator.SellableStock(lots[p.Id], today)).ToString(Bo)));
    }

    /// <summary>RF-071: productos con stock en el mínimo o por debajo.</summary>
    private static async Task<ReportTableDto> LowStockAsync(BusinessDb db, TimeProvider time)
    {
        var today = BusinessEndpoints.Today(time);
        var low = InventoryAlerts.LowStock(await db.Products.ToListAsync(), await db.Lots.ToListAsync(), today);
        return new ReportTableDto("Productos con stock bajo", $"Al {today:dd/MM/yyyy}",
            ["Producto", "Stock vendible", "Mínimo", "Faltan para el mínimo"],
            low.Select(a => Cells(a.ProductName, a.Stock.ToString(Bo), a.MinimumStock.ToString(Bo),
                Math.Max(0, a.MinimumStock - a.Stock).ToString(Bo))).ToList(),
            Cells($"{low.Count} productos"), low.Count == 0 ? "Ningún producto está en su stock mínimo." : "");
    }

    /// <summary>RF-072: lotes vencidos o que vencen dentro del umbral configurado (SCRUM-130).</summary>
    private static async Task<ReportTableDto> ExpiringAsync(BusinessDb db, TimeProvider time)
    {
        var today = BusinessEndpoints.Today(time);
        var settings = await OperationalSettings.GetAsync(db);
        var alerts = InventoryAlerts.Expiry(await db.Products.ToListAsync(), await db.Lots.ToListAsync(), today,
            settings.ExpiryThresholdDays);
        return new ReportTableDto("Medicamentos próximos a vencer",
            $"Al {today:dd/MM/yyyy} · umbral {settings.ExpiryThresholdDays} días",
            ["Producto", "Lote", "Vencimiento", "Días", "Cantidad", "Estado"],
            alerts.Select(e => Cells(e.ProductName, e.LotNumber, e.ExpirationDate.ToString("dd/MM/yyyy"),
                e.DaysToExpire.ToString(Bo), e.Quantity.ToString(Bo),
                e.Status == ExpiryStatus.Vencido ? "Vencido" : "Por vencer")).ToList(),
            Cells($"{alerts.Count} lotes", "", "", "", alerts.Sum(e => e.Quantity).ToString(Bo)),
            alerts.Count == 0 ? "No hay lotes vencidos ni por vencer." : "");
    }

    /// <summary>RF-103: cierre diario con las ventas, los egresos y el saldo de caja de todos los turnos del día.</summary>
    private static async Task<ReportTableDto> DailyClosingAsync(BusinessDb db, DateOnly day, string? branch)
    {
        var start = Local(day);
        var end = Local(day.AddDays(1));
        var sessions = (await db.CashSessions.Where(s => s.OpenedAt >= start && s.OpenedAt < end).ToListAsync())
            .Where(s => string.IsNullOrWhiteSpace(branch) || s.Branch == branch)
            .OrderBy(s => s.OpenedAt).ToList();
        var ids = sessions.Select(s => s.Id).ToList();
        var movements = (await db.CashMovements.Where(m => ids.Contains(m.CashSessionId)).ToListAsync())
            .ToLookup(m => m.CashSessionId);

        var rows = new List<IReadOnlyList<string>>();
        decimal sales = 0, expenses = 0, balance = 0;
        foreach (var s in sessions)
        {
            var sessionSales = s.SalesTotal ?? movements[s.Id].Where(m => m.Type == "Venta").Sum(m => m.Amount);
            var sessionExpenses = s.ExpensesTotal ?? movements[s.Id].Where(m => m.Type != "Venta").Sum(m => m.Amount);
            var expected = s.ExpectedAmount ?? s.OpeningAmount + sessionSales - sessionExpenses;
            var saldo = s.CountedAmount ?? expected;
            sales += sessionSales;
            expenses += sessionExpenses;
            balance += saldo;
            rows.Add(Cells(s.CashierName, s.DeviceId, s.Branch, s.OpenedAt.ToOffset(BusinessEndpoints.LaPaz).ToString("HH:mm"),
                s.ClosedAt?.ToOffset(BusinessEndpoints.LaPaz).ToString("HH:mm") ?? "abierta", Money(s.OpeningAmount),
                Money(sessionSales), Money(sessionExpenses), Money(expected),
                s.CountedAmount is null ? "—" : Money(s.CountedAmount.Value),
                s.Difference is null ? "—" : Money(s.Difference.Value), s.Status));
        }
        var open = sessions.Count(s => s.ClosedAt is null);
        return new ReportTableDto("Cierre diario consolidado", $"{day:dd/MM/yyyy}" +
                (string.IsNullOrWhiteSpace(branch) ? "" : $" · sucursal {branch}") +
                $" · {sessions.Count} turnos" + (open > 0 ? $" ({open} sin cerrar)" : ""),
            ["Cajero", "Puesto", "Sucursal", "Apertura", "Cierre", "Inicial", "Ventas", "Egresos", "Esperado", "Contado",
                "Diferencia", "Estado"], rows,
            sessions.Count == 0 ? [] : Cells("Total del día", "", "", "", "", "", Money(sales), Money(expenses), "",
                $"Saldo {Money(balance)}"),
            sessions.Count == 0 ? "No hubo turnos de caja ese día." : "");
    }

    private static string Period(DateOnly from, DateOnly to, string? branch) =>
        $"Del {from:dd/MM/yyyy} al {to:dd/MM/yyyy}" + (string.IsNullOrWhiteSpace(branch) ? "" : $" · sucursal {branch}");

    private static DateTimeOffset Local(DateOnly day) =>
        new(day.ToDateTime(TimeOnly.MinValue), BusinessEndpoints.LaPaz);

    private static string When(DateTimeOffset value) => value.ToOffset(BusinessEndpoints.LaPaz).ToString("dd/MM/yyyy HH:mm");

    private static string Money(decimal value) => value.ToString("N2", Bo);

    private static IReadOnlyList<string> Cells(params string[] cells) => cells;

    /// <summary>La misma tabla como hoja de Excel (RF-105).</summary>
    public static XlsxSheet ToSheet(ReportTableDto table)
    {
        var rows = table.Rows.Select(r => (IReadOnlyList<object?>)r.Select(Cell).ToList()).ToList();
        if (table.Totals.Count > 0) rows.Add(table.Totals.Select(Cell).ToList());
        if (rows.Count == 0) rows.Add([table.Notice]);
        var name = table.Title.Length > 31 ? table.Title[..31] : table.Title;
        return new XlsxSheet(name, table.Columns, rows);

        // Los montos y cantidades se exportan como números para poder sumarlos en Excel.
        static object? Cell(string value) =>
            decimal.TryParse(value, NumberStyles.Number, Bo, out var number) && value.Any(char.IsDigit)
            && !value.Contains('/') && !value.Contains(':') ? number : value;
    }
}
