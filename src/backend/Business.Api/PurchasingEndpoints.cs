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
        MapPurchases(app);
        MapPrices(app);
    }

    /// <summary>HU-10.1 (RF-034, RF-035) y HU-10.2 (RF-036): precios de compra por proveedor.</summary>
    private static void MapPrices(WebApplication app)
    {
        var prices = app.MapGroup("/api").RequireAnyPermission(Permission.Compras, Permission.Proveedores)
            .WithTags("Precios de proveedores");

        prices.MapPost("/suppliers/{id:guid}/prices", async (Guid id, RegisterSupplierPriceRequest r, BusinessDb db,
                HttpContext http, TimeProvider time) =>
            {
                var supplier = await db.Suppliers.FindAsync(id);
                if (supplier is null) return Results.NotFound();
                var product = await db.Products.FindAsync(r.ProductId)
                    ?? throw new DomainException("El producto no existe en el catálogo.");

                var price = new SupplierPrice(Guid.NewGuid(), id, product.Id, r.Price, UserName(http), time.GetUtcNow());
                db.SupplierPrices.Add(price);
                Audit(db, http, time, "Precio de proveedor", $"{supplier.Name} · {product.Code}: Bs {price.Price:0.00}");
                await db.SaveChangesAsync();
                return Results.Created($"/api/suppliers/{id}/prices", ToDto(price, supplier.Name, product.DisplayName));
            })
            .WithSummary("Registra el precio que el proveedor ofrece por un producto (RF-034)");

        prices.MapGet("/suppliers/{id:guid}/prices", async (Guid id, BusinessDb db) =>
            {
                var supplier = await db.Suppliers.FindAsync(id);
                if (supplier is null) return Results.NotFound();
                var names = await ProductNamesAsync(db);
                var current = SupplierPrice.Current(await db.SupplierPrices.Where(p => p.SupplierId == id).ToListAsync());
                return Results.Ok(current
                    .Select(p => ToDto(p, supplier.Name, names.GetValueOrDefault(p.ProductId, "?")))
                    .OrderBy(p => p.ProductName)
                    .ToList());
            })
            .WithSummary("Precios vigentes del proveedor por producto (RF-034)");

        prices.MapGet("/suppliers/{id:guid}/prices/history", async (Guid id, Guid productId, BusinessDb db) =>
            {
                var supplier = await db.Suppliers.FindAsync(id);
                if (supplier is null) return Results.NotFound();
                var names = await ProductNamesAsync(db);
                var history = await db.SupplierPrices.Where(p => p.SupplierId == id && p.ProductId == productId)
                    .ToListAsync();
                return Results.Ok(history.OrderByDescending(p => p.RegisteredAt)
                    .Select(p => ToDto(p, supplier.Name, names.GetValueOrDefault(p.ProductId, "?")))
                    .ToList());
            })
            .WithSummary("Historial de precios de un producto con el proveedor (RF-035)");
    }

    private static async Task<Dictionary<Guid, string>> ProductNamesAsync(BusinessDb db) =>
        (await db.Products.ToListAsync()).ToDictionary(p => p.Id, p => p.DisplayName);

    private static SupplierPriceDto ToDto(SupplierPrice p, string supplierName, string productName) =>
        new(p.SupplierId, supplierName, p.ProductId, productName, p.Price, p.RegisteredAt, p.RegisteredBy);

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

        suppliers.MapGet("/{id:guid}/products", async (Guid id, BusinessDb db) =>
            {
                if (await db.Suppliers.FindAsync(id) is null) return Results.NotFound();
                var received = (await db.Purchases
                        .Where(p => p.SupplierId == id && p.Status == PurchaseStatus.Recibida).ToListAsync())
                    .OrderBy(p => p.CreatedAt);
                var products = received
                    .SelectMany(p => p.Lines.Where(l => l.ReceivedQuantity > 0).Select(l => (Purchase: p, Line: l)))
                    .GroupBy(x => x.Line.ProductId)
                    .Select(g =>
                    {
                        var last = g.Last();
                        return new SupplierProductDto(g.Key, last.Line.ProductName,
                            g.Sum(x => x.Line.ReceivedQuantity), last.Line.UnitCost, last.Purchase.CreatedAt);
                    })
                    .OrderBy(p => p.ProductName)
                    .ToList();
                return Results.Ok(products);
            })
            .RequireAnyPermission(Permission.Proveedores, Permission.Compras)
            .WithSummary("Productos que suministró el proveedor según las compras recibidas (RF-028)");

        suppliers.MapGet("/{id:guid}/purchases", async (Guid id, BusinessDb db) =>
            {
                if (await db.Suppliers.FindAsync(id) is null) return Results.NotFound();
                var purchases = await db.Purchases.Where(p => p.SupplierId == id).ToListAsync();
                return Results.Ok(purchases.OrderByDescending(p => p.CreatedAt).Select(ToDto).ToList());
            })
            .RequireAnyPermission(Permission.Proveedores, Permission.Compras)
            .WithSummary("Historial de compras del proveedor con fecha, productos y monto (RF-029)");
    }

    private static void MapPurchases(WebApplication app)
    {
        var purchases = app.MapGroup("/api/purchases").RequirePermission(Permission.Compras).WithTags("Compras");

        purchases.MapGet("/", async (string? status, BusinessDb db) =>
            {
                var all = await db.Purchases.ToListAsync();
                return all
                    .Where(p => string.IsNullOrEmpty(status)
                                || p.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(ToDto)
                    .ToList();
            })
            .WithSummary("Lista las compras, opcionalmente por estado (Pendiente o Recibida)");

        purchases.MapGet("/{id:guid}", async (Guid id, BusinessDb db) =>
                await db.Purchases.SingleOrDefaultAsync(p => p.Id == id) is { } purchase
                    ? Results.Ok(ToDto(purchase))
                    : Results.NotFound())
            .WithSummary("Detalle de una compra");

        purchases.MapPost("/", async (CreatePurchaseRequest r, BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                if (r.Items is not { Count: > 0 })
                    throw new DomainException("Agregue al menos un producto a la compra.");
                var supplier = await db.Suppliers.FindAsync(r.SupplierId)
                    ?? throw new DomainException("Seleccione un proveedor registrado.");

                var ids = r.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
                var number = $"OC-{await db.Purchases.CountAsync() + 1:D5}";
                var purchase = Purchase.Create(Guid.NewGuid(), number, supplier, UserName(http), time.GetUtcNow());
                foreach (var item in r.Items)
                {
                    if (!products.TryGetValue(item.ProductId, out var product))
                        throw new DomainException("Uno de los productos no existe en el catálogo.");
                    purchase.AddLine(product, item.Quantity, item.UnitCost);
                }

                db.Purchases.Add(purchase);
                Audit(db, http, time, "Registro de compra",
                    $"{purchase.Number} · {supplier.Name} · Bs {purchase.Total:0.00}");
                await db.SaveChangesAsync();
                return Results.Created($"/api/purchases/{purchase.Id}", ToDto(purchase));
            })
            .WithSummary("Registra una compra y calcula su total (RF-030, RF-031)");

        purchases.MapPost("/{id:guid}/receive", async (Guid id, ReceivePurchaseRequest r, BusinessDb db,
                HttpContext http, TimeProvider time) =>
            {
                var purchase = await db.Purchases.SingleOrDefaultAsync(p => p.Id == id);
                if (purchase is null) return Results.NotFound();

                var now = time.GetUtcNow();
                var userName = UserName(http);
                var received = purchase.Receive(
                    r.Lines.Select(l => new ReceptionItem(l.LineId, l.LotNumber, l.ExpirationDate, l.ReceivedQuantity,
                        l.DamagedQuantity)).ToList(),
                    userName, now, BusinessEndpoints.Today(time));

                foreach (var (line, lot) in received)
                {
                    // RF-033: solo ingresan al stock las unidades en buen estado.
                    db.Lots.Add(lot);
                    db.StockMovements.Add(new StockMovement(Guid.NewGuid(), lot.ProductId, lot.Id,
                        StockMovementType.Compra, lot.Quantity, $"Recepción {purchase.Number}", userName, now));
                    db.LotFeed.Add(new LotFeedEntry
                    {
                        LotId = lot.Id, ProductId = lot.ProductId, LotNumber = lot.LotNumber,
                        ExpirationDate = lot.ExpirationDate, Quantity = lot.Quantity, CreatedAt = now,
                    });
                }

                var differences = purchase.Lines.Where(l => l.Difference != 0)
                    .Select(l => $"{l.ProductName}: {l.ReceivedQuantity}/{l.Quantity} (dañadas {l.DamagedQuantity}, faltan {l.MissingQuantity})")
                    .ToList();
                Audit(db, http, time, "Recepción de compra", differences.Count == 0
                    ? $"{purchase.Number}: recepción completa"
                    : $"{purchase.Number}: {string.Join("; ", differences)}");
                await db.SaveChangesAsync();
                return Results.Ok(ToDto(purchase));
            })
            .WithSummary("Registra la recepción con lotes y diferencias; actualiza el stock (RF-032, RF-033, RF-107, RF-108)");
    }

    public static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.TaxId, s.Name, s.ContactName, s.Phone, s.Email, s.Address, s.IsActive);

    public static PurchaseDto ToDto(Purchase p) => new(p.Id, p.Number, p.SupplierId, p.SupplierName,
        p.Status.ToString(), p.CreatedAt, p.CreatedBy, p.Total, p.ReceivedTotal, p.ReceivedAt, p.ReceivedBy,
        p.Lines.Select(l => new PurchaseLineDto(l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitCost, l.Subtotal,
            l.ReceivedQuantity, l.DamagedQuantity, l.MissingQuantity, l.Difference, l.LotNumber, l.ExpirationDate))
            .ToList());

    private static string UserName(HttpContext http) => http.User.FindFirst("name")?.Value ?? "?";

    private static void Audit(BusinessDb db, HttpContext http, TimeProvider time, string action, string detail) =>
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = UserName(http),
            DeviceId = http.User.FindFirst("device")?.Value ?? "", Action = action, Detail = detail,
        });
}
