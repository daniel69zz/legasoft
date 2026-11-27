using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>SCRUM-34 · HU-29 (RF-100): sucursal de cada puesto y resumen por sucursal.</summary>
public static partial class Sprint3Endpoints
{
    private static void MapBranches(RouteGroupBuilder api)
    {
        // SCRUM-34 · HU-29 (RF-100): sucursal de cada puesto y resumen por sucursal (requiere conexión).
        api.MapGet("/branches/devices", async (BusinessDb db) => await Branches.DevicesAsync(db))
            .RequirePermission(Permission.Reportes)
            .WithTags("Sucursales")
            .WithSummary("[SCRUM-34 · HU-29] Puestos asignados a cada sucursal (RF-100)");

        api.MapPut("/branches/devices", async (BranchDeviceDto request, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                if (string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.Branch))
                    throw new DomainException("Indique el puesto y la sucursal.");
                await Branches.AssignAsync(db, request, UserName(http), time.GetUtcNow());
                Audit(db, http, time, "Cambio de sucursal", $"{request.DeviceId.ToUpperInvariant()} → {request.Branch}");
                await db.SaveChangesAsync();
                return Results.Ok(request);
            })
            .RequireAuthorization(p => p.RequireRole(Roles.Administrador))
            .WithTags("Sucursales")
            .WithSummary("[SCRUM-34 · HU-29] Asigna un puesto a una sucursal (RF-100)");

        api.MapGet("/branches/{branch}/summary", async (string branch, DateOnly? from, DateOnly? to, BusinessDb db,
                TimeProvider time) =>
            {
                var r = OperationsReports.Range(from, to, time);
                var sales = await db.Sales.Where(s => s.Branch == branch && s.Status == "Completada"
                    && s.CreatedAt >= r.Start && s.CreatedAt < r.End).ToListAsync();
                var sessions = await db.CashSessions.CountAsync(s => s.Branch == branch
                    && s.OpenedAt >= r.Start && s.OpenedAt < r.End);
                var products = await db.Products.ToDictionaryAsync(p => p.Id);
                var movements = await db.StockMovements
                    .Where(m => EF.Property<string>(m, Branches.Column) == branch)
                    .Where(m => m.OccurredAt >= r.Start && m.OccurredAt < r.End)
                    .ToListAsync();
                var devices = (await Branches.DevicesAsync(db)).Where(d => d.Branch == branch).Select(d => d.DeviceId)
                    .ToList();
                return new BranchSummaryDto(branch, devices, sales.Count, sales.Sum(s => s.Total), sessions,
                    movements.GroupBy(m => m.ProductId)
                        .Select(g => new BranchStockDto(products.GetValueOrDefault(g.Key)?.Code ?? "?",
                            products.GetValueOrDefault(g.Key)?.Name ?? "?", g.Sum(m => m.Quantity)))
                        .OrderBy(x => x.ProductName).ToList());
            })
            .RequirePermission(Permission.Reportes)
            .WithTags("Sucursales")
            .WithSummary("[SCRUM-34 · HU-29] Ventas, turnos y movimiento neto de stock de una sucursal (RF-100)")
            .Produces<BranchSummaryDto>();
    }
}
