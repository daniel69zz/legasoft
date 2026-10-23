using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>
/// HU-05: catálogo central de medicamentos (RF-009 a RF-012). Los cambios llegan a los
/// puestos con la descarga incremental del Sync Worker, porque cada uno actualiza UpdatedAt.
/// </summary>
public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var catalog = app.MapGroup("/api/catalog").RequirePermission(Permission.Catalogo).WithTags("Catálogo");

        catalog.MapGet("/", async (string? search, bool? includeInactive, BusinessDb db) =>
            (await db.Products.OrderBy(p => p.Name).ToListAsync())
                .Where(p => ((includeInactive ?? true) || p.IsActive) && p.Matches(search ?? ""))
                .Select(ToDto)
                .ToList())
            .WithSummary("Busca medicamentos por nombre, código, principio activo o categoría (RF-012)");

        catalog.MapPost("/", async (SaveProductRequest r, BusinessDb db, HttpContext http, TimeProvider time) =>
        {
            var code = r.Code.Trim().ToUpperInvariant();
            if (await db.Products.AnyAsync(p => p.Code == code))
                return Results.Problem($"Ya existe un medicamento con el código {code}.", statusCode: 409);

            var product = new Product(Guid.NewGuid(), code, r.Name, r.ActiveIngredient.Trim(), r.Concentration.Trim(),
                r.Presentation.Trim(), r.Laboratory.Trim(), r.Category.Trim(), r.RequiresPrescription, r.SalePrice,
                r.MinimumStock, r.Location.Trim());
            db.Products.Add(product);
            Audit(db, http, time, "Alta de medicamento", $"{product.Code} · {product.DisplayName}");
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/{product.Id}", ToDto(product));
        }).WithSummary("Registra un medicamento (RF-009)");

        catalog.MapPut("/{id:guid}", async (Guid id, SaveProductRequest r, BusinessDb db, HttpContext http,
            TimeProvider time) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.UpdateDetails(r.Name, r.ActiveIngredient, r.Concentration, r.Presentation, r.Laboratory,
                r.Category, r.RequiresPrescription, r.SalePrice, r.MinimumStock, r.Location, time.GetUtcNow());
            Audit(db, http, time, "Modificación de medicamento", $"{product.Code} · {product.DisplayName}");
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(product));
        }).WithSummary("Modifica los datos de un medicamento (RF-010)");

        catalog.MapPost("/{id:guid}/{action:regex(^(activate|deactivate)$)}", async (Guid id, string action,
            BusinessDb db, HttpContext http, TimeProvider time) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.SetActive(action == "activate", time.GetUtcNow());
            Audit(db, http, time, product.IsActive ? "Activación de medicamento" : "Desactivación de medicamento",
                product.Code);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithSummary("Activa o desactiva un medicamento (RF-011)");
    }

    private static ProductDto ToDto(Product p) => new(p.Id, p.Code, p.Name, p.ActiveIngredient, p.Concentration,
        p.Presentation, p.Laboratory, p.Category, p.RequiresPrescription, p.SalePrice, p.MinimumStock, p.Location,
        p.IsActive);

    private static void Audit(BusinessDb db, HttpContext http, TimeProvider time, string action, string detail) =>
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = http.User.FindFirst("name")?.Value ?? "?",
            DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = action, Detail = detail,
        });
}
