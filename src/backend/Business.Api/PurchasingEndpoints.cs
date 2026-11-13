using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Purchasing;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>
/// HU-08 (proveedores, RF-026 a RF-029) y HU-09 (compras y recepción, RF-030 a RF-033, RF-020,
/// RF-107 y RF-108). Se administran en línea; los lotes recibidos llegan a los puestos con la
/// descarga incremental de lotes (/internal/lots).
/// </summary>
public static class PurchasingEndpoints
{
    public static void MapPurchasingEndpoints(this WebApplication app)
    {
        MapSuppliers(app);
    }

    private static void MapSuppliers(WebApplication app)
    {
        var suppliers = app.MapGroup("/api/suppliers").WithTags("Proveedores");

        // Quien registra compras necesita elegir el proveedor, aunque no pueda modificarlo.
        suppliers.MapGet("/", async (string? search, bool? includeInactive, BusinessDb db) =>
                (await db.Suppliers.OrderBy(s => s.Name).ToListAsync())
                .Where(s => ((includeInactive ?? true) || s.IsActive) && s.Matches(search ?? ""))
                .Select(ToDto)
                .ToList())
            .RequireAnyPermission(Permission.Proveedores, Permission.Compras)
            .WithSummary("Lista y busca proveedores por razón social, NIT o contacto");

        suppliers.MapPost("/", async (SaveSupplierRequest r, BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                var taxId = r.TaxId.Trim();
                if (taxId.Length > 0 && await db.Suppliers.AnyAsync(s => s.TaxId == taxId))
                    return Results.Problem($"Ya existe un proveedor con el NIT {taxId}.", statusCode: 409);

                var supplier = new Supplier(Guid.NewGuid(), taxId, r.Name, r.ContactName, r.Phone, r.Email,
                    r.Address, time.GetUtcNow());
                db.Suppliers.Add(supplier);
                Audit(db, http, time, "Alta de proveedor", $"{supplier.TaxId} · {supplier.Name}");
                await db.SaveChangesAsync();
                return Results.Created($"/api/suppliers/{supplier.Id}", ToDto(supplier));
            })
            .RequirePermission(Permission.Proveedores)
            .WithSummary("Registra un proveedor (RF-026)");

        suppliers.MapPut("/{id:guid}", async (Guid id, SaveSupplierRequest r, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                var supplier = await db.Suppliers.FindAsync(id);
                if (supplier is null) return Results.NotFound();
                supplier.Update(r.Name, r.ContactName, r.Phone, r.Email, r.Address, time.GetUtcNow());
                Audit(db, http, time, "Modificación de proveedor", $"{supplier.TaxId} · {supplier.Name}");
                await db.SaveChangesAsync();
                return Results.Ok(ToDto(supplier));
            })
            .RequirePermission(Permission.Proveedores)
            .WithSummary("Modifica los datos de un proveedor (RF-027)");

        suppliers.MapPost("/{id:guid}/{action:regex(^(activate|deactivate)$)}", async (Guid id, string action,
                BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                var supplier = await db.Suppliers.FindAsync(id);
                if (supplier is null) return Results.NotFound();
                supplier.SetActive(action == "activate", time.GetUtcNow());
                Audit(db, http, time, supplier.IsActive ? "Activación de proveedor" : "Desactivación de proveedor",
                    supplier.Name);
                await db.SaveChangesAsync();
                return Results.NoContent();
            })
            .RequirePermission(Permission.Proveedores)
            .WithSummary("Activa o desactiva un proveedor");
    }

    public static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.TaxId, s.Name, s.ContactName, s.Phone, s.Email, s.Address, s.IsActive);

    private static string UserName(HttpContext http) => http.User.FindFirst("name")?.Value ?? "?";

    private static void Audit(BusinessDb db, HttpContext http, TimeProvider time, string action, string detail) =>
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = UserName(http),
            DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = action, Detail = detail,
        });
}
