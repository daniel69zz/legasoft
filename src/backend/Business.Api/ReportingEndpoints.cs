using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>
/// Consultas y configuración incorporadas al Sprint 2: historial general de movimientos (HU-06.4),
/// auditoría con filtros (HU-20.1), exportación a Excel (HU-18.1) y parámetros operativos (HU-28.1).
/// </summary>
public static class ReportingEndpoints
{
    public const int MaxRows = 1000;

    public static void MapReportingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/inventory/movements", async (DateOnly? from, DateOnly? to, string? type, Guid? productId,
                BusinessDb db, TimeProvider time) =>
            {
                var (start, end) = Period(from, to, time);
                var query = db.StockMovements.Where(m => m.OccurredAt >= start && m.OccurredAt < end);
                if (productId is { } id) query = query.Where(m => m.ProductId == id);
                if (!string.IsNullOrWhiteSpace(type))
                {
                    if (!Enum.TryParse<StockMovementType>(type, ignoreCase: true, out var parsed))
                        return Results.Problem($"Tipo de movimiento desconocido: {type}.", statusCode: 400);
                    query = query.Where(m => m.Type == parsed);
                }

                var movements = await query.OrderByDescending(m => m.OccurredAt).Take(MaxRows).ToListAsync();
                var products = await db.Products.ToDictionaryAsync(p => p.Id);
                var lotIds = movements.Select(m => m.LotId).Distinct().ToList();
                var lots = await db.Lots.Where(l => lotIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.LotNumber);
                return Results.Ok(movements.Select(m =>
                {
                    var product = products.GetValueOrDefault(m.ProductId);
                    return new InventoryMovementDto(m.OccurredAt, m.ProductId, product?.Code ?? "?",
                        product?.DisplayName ?? "Producto desconocido", lots.GetValueOrDefault(m.LotId, "?"),
                        m.Type.ToString(), m.Quantity, m.Reason, m.UserName);
                }).ToList());
            })
            .RequirePermission(Permission.Inventario)
            .WithTags("Inventario")
            .WithSummary("Historial general de movimientos por período, tipo y producto (RF-019)");

        api.MapGet("/audit", async (DateOnly? from, DateOnly? to, string? user, string? action, BusinessDb db,
                TimeProvider time) =>
            {
                var query = db.Audit.AsQueryable();
                if (from is not null || to is not null)
                {
                    var (start, end) = Period(from, to, time);
                    query = query.Where(a => a.OccurredAt >= start && a.OccurredAt < end);
                }
                var entries = await query.OrderByDescending(a => a.OccurredAt).Take(MaxRows).ToListAsync();
                return entries
                    .Where(a => string.IsNullOrWhiteSpace(user) || a.UserName.Contains(user.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Where(a => string.IsNullOrWhiteSpace(action) || a.Action.Contains(action.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Take(200)
                    .Select(a => new AuditEntryDto(a.OccurredAt, a.UserName, a.Action, a.Detail, a.DeviceId))
                    .ToList();
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador, Roles.GerenteGeneral, Roles.Contadora))
            .WithTags("Auditoría")
            .WithSummary("Auditoría de operaciones críticas filtrada por período, usuario y operación (RF-075)");

        api.MapGet("/reports/export", async (DateOnly? from, DateOnly? to, BusinessDb db, TimeProvider time,
                HttpContext http) =>
            {
                var (start, end) = Period(from, to, time);
                var sheets = await ExportSheetsAsync(db, start, end);
                var empty = sheets.All(s => s.Rows.Count == 1); // solo la fila del aviso
                if (empty) http.Response.Headers["X-Export-Empty"] = "true";
                var name = $"farmacia-{DateOnly.FromDateTime(start.DateTime):yyyyMMdd}-{DateOnly.FromDateTime(end.DateTime).AddDays(-1):yyyyMMdd}.xlsx";
                return Results.File(XlsxWriter.Write(sheets), XlsxWriter.ContentType, name);
            })
            .RequirePermission(Permission.Reportes)
            .WithTags("Reportes")
            .WithSummary("Exporta a Excel las ventas, los egresos y los cierres de caja del período (RF-105)");

        api.MapGet("/settings", async (BusinessDb db) => await OperationalSettings.GetAsync(db))
            .RequireAuthorization()
            .WithTags("Configuración")
            .WithSummary("Parámetros operativos vigentes (RF-099)");

        api.MapPut("/settings", async (OperationalSettingsDto request, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                OperationalSettings.Validate(request);
                var before = await OperationalSettings.GetAsync(db);
                var userName = http.User.FindFirst("name")?.Value ?? "?";
                await OperationalSettings.SaveAsync(db, request, userName, time.GetUtcNow());
                db.Audit.Add(new AuditEntry
                {
                    Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = userName,
                    DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = "Cambio de parámetros",
                    Detail = $"Umbral de vencimiento {before.ExpiryThresholdDays} → {request.ExpiryThresholdDays} días; " +
                             $"alertas cada {before.AlertIntervalMinutes} → {request.AlertIntervalMinutes} min",
                });
                await db.SaveChangesAsync();
                return Results.Ok(request);
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador))
            .WithTags("Configuración")
            .WithSummary("Modifica los parámetros operativos de inventario (RF-099)");
    }

    /// <summary>Período en hora de La Paz; por defecto, los últimos 7 días incluido hoy.</summary>
    private static (DateTimeOffset Start, DateTimeOffset End) Period(DateOnly? from, DateOnly? to, TimeProvider time)
    {
        var last = to ?? BusinessEndpoints.Today(time);
        var first = from ?? last.AddDays(-6);
        if (first > last) (first, last) = (last, first);
        return (new DateTimeOffset(first.ToDateTime(TimeOnly.MinValue), BusinessEndpoints.LaPaz),
            new DateTimeOffset(last.AddDays(1).ToDateTime(TimeOnly.MinValue), BusinessEndpoints.LaPaz));
    }

    private static async Task<IReadOnlyList<XlsxSheet>> ExportSheetsAsync(BusinessDb db, DateTimeOffset start,
        DateTimeOffset end)
    {
        var sales = (await db.Sales.Where(s => s.CreatedAt >= start && s.CreatedAt < end).ToListAsync())
            .OrderBy(s => s.CreatedAt).ToList();
        var expenses = (await db.CashMovements
                .Where(m => m.Type != "Venta" && m.OccurredAt >= start && m.OccurredAt < end).ToListAsync())
            .OrderBy(m => m.OccurredAt).ToList();
        var closures = (await db.CashSessions
                .Where(s => s.ClosedAt != null && s.ClosedAt >= start && s.ClosedAt < end).ToListAsync())
            .OrderBy(s => s.ClosedAt).ToList();

        var salesRows = sales.Select(s => Row(Local(s.CreatedAt), s.ReceiptNumber, s.CashierName, s.DeviceId,
            s.Status, s.Subtotal, s.Discount, s.Total)).ToList();
        var completed = sales.Where(s => s.Status == "Completada").ToList();
        salesRows.Add(Row("Total (completadas)", null, null, null, null, completed.Sum(s => s.Subtotal),
            completed.Sum(s => s.Discount), completed.Sum(s => s.Total)));

        var expenseRows = expenses.Select(m => Row(Local(m.OccurredAt), m.Type, m.Category, m.Reason, m.UserName,
            m.Amount)).ToList();
        expenseRows.Add(Row("Total", null, null, null, null, expenses.Sum(m => m.Amount)));

        var closureRows = closures.Select(s => Row(Local(s.OpenedAt), Local(s.ClosedAt!.Value), s.CashierName,
            s.DeviceId, s.OpeningAmount, s.SalesTotal, s.ExpensesTotal, s.ExpectedAmount, s.CountedAmount,
            s.Difference, s.Justification)).ToList();
        closureRows.Add(Row("Total", null, null, null, null, closures.Sum(s => s.SalesTotal ?? 0),
            closures.Sum(s => s.ExpensesTotal ?? 0), null, null, closures.Sum(s => s.Difference ?? 0)));

        // Período sin datos: hojas con el aviso en lugar de las filas de totales en cero.
        const string none = "Sin operaciones en el período";
        if (sales.Count == 0) salesRows = [Row(none)];
        if (expenses.Count == 0) expenseRows = [Row(none)];
        if (closures.Count == 0) closureRows = [Row(none)];

        return
        [
            new("Ventas", ["Fecha", "Recibo", "Cajero", "Puesto", "Estado", "Subtotal", "Descuento", "Total"], salesRows),
            new("Egresos", ["Fecha", "Tipo", "Categoría", "Motivo", "Usuario", "Monto"], expenseRows),
            new("Cierres de caja", ["Apertura", "Cierre", "Cajero", "Puesto", "Inicial", "Ventas", "Egresos",
                "Esperado", "Contado", "Diferencia", "Justificación"], closureRows),
        ];
    }

    private static IReadOnlyList<object?> Row(params object?[] cells) => cells;

    private static DateTimeOffset Local(DateTimeOffset value) => value.ToOffset(BusinessEndpoints.LaPaz);
}
