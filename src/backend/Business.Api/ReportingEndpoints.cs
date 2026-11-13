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
}
