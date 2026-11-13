using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

public sealed record ChangePriceRequest(decimal SalePrice);

public sealed record ReceiveLotRequest(Guid ProductId, string LotNumber, DateOnly ExpirationDate, int Quantity);

public static class BusinessEndpoints
{
    public static void MapBusinessEndpoints(this WebApplication app)
    {
        MapInternal(app);
        app.MapCatalogEndpoints();
        app.MapPurchasingEndpoints();
        app.MapReportingEndpoints();

        var api = app.MapGroup("/api");

        api.MapGet("/products", async (BusinessDb db, TimeProvider time) =>
        {
            var today = Today(time);
            var products = await db.Products.OrderBy(p => p.Name).ToListAsync();
            var lots = (await db.Lots.ToListAsync()).ToLookup(l => l.ProductId);
            return products.Select(p => new
            {
                p.Id, p.Code, p.Name, p.Concentration, p.Presentation, p.Category, p.SalePrice, p.MinimumStock,
                p.IsActive, Stock = FefoAllocator.SellableStock(lots[p.Id], today),
            });
        }).RequirePermission(Permission.Inventario).WithTags("Inventario")
            .WithSummary("Productos con su stock vendible (RF-013)");

        // El cambio de precio llega a los puestos con la siguiente descarga incremental.
        api.MapPut("/products/{id:guid}/price", async (Guid id, ChangePriceRequest request, BusinessDb db,
            HttpContext http, TimeProvider time) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) return Results.NotFound();
            var before = product.SalePrice;
            product.ChangePrice(request.SalePrice, time.GetUtcNow());
            db.Audit.Add(new AuditEntry
            {
                Id = Guid.NewGuid(), OccurredAt = time.GetUtcNow(), UserName = http.User.FindFirst("name")?.Value ?? "?",
                Action = "Cambio de precio", Detail = $"{product.Code}: Bs {before:0.00} → Bs {product.SalePrice:0.00}",
            });
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequirePermission(Permission.Catalogo).WithTags("Catálogo")
            .WithSummary("Cambia el precio de venta de un medicamento (RF-010)")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapPost("/lots", async (ReceiveLotRequest r, BusinessDb db, HttpContext http, TimeProvider time) =>
        {
            if (await db.Products.FindAsync(r.ProductId) is null) return Results.NotFound();
            var lot = new Lot(Guid.NewGuid(), r.ProductId, r.LotNumber, r.ExpirationDate, r.Quantity);
            db.Lots.Add(lot);
            db.StockMovements.Add(new StockMovement(Guid.NewGuid(), r.ProductId, lot.Id, StockMovementType.Compra,
                r.Quantity, $"Recepción del lote {r.LotNumber}", http.User.FindFirst("name")?.Value ?? "?",
                time.GetUtcNow()));
            // SCRUM-19 · HU-09: el lote se publica para los puestos.
            db.LotFeed.Add(new LotFeedEntry
            {
                LotId = lot.Id, ProductId = lot.ProductId, LotNumber = lot.LotNumber,
                ExpirationDate = lot.ExpirationDate, Quantity = lot.Quantity, CreatedAt = time.GetUtcNow(),
            });
            await db.SaveChangesAsync();
            return Results.Created($"/api/lots/{lot.Id}", new { lot.Id });
        }).RequirePermission(Permission.Inventario).WithTags("Inventario")
            .WithSummary("Registra un lote recibido fuera de una compra (RF-020)")
            .Produces(StatusCodes.Status201Created).ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/reports/sales", async (DateOnly? from, DateOnly? to, BusinessDb db, TimeProvider time) =>
        {
            var end = to ?? Today(time);
            var start = from ?? end.AddDays(-6);
            return await SalesByCashierAsync(db, start, end);
        }).RequirePermission(Permission.Reportes).WithTags("Reportes")
            .WithSummary("Ventas por cajero y día del período (RF-074)");

        api.MapGet("/dashboard", async (BusinessDb db, TimeProvider time) =>
        {
            var today = Today(time);
            var last7 = await SalesByCashierAsync(db, today.AddDays(-6), today);
            var products = await db.Products.ToListAsync();
            var lots = await db.Lots.ToListAsync();
            var settings = await OperationalSettings.GetAsync(db);
            var expiry = InventoryAlerts.Expiry(products, lots, today, settings.ExpiryThresholdDays); // SCRUM-130
            return new DashboardSummary(
                SalesToday: last7.Where(r => r.Day == today).Sum(r => r.Total),
                SalesCountToday: last7.Where(r => r.Day == today).Sum(r => r.Sales),
                SalesLast7Days: last7.Sum(r => r.Total),
                LowStockProducts: InventoryAlerts.LowStock(products, lots, today).Count,
                ExpiringLots: expiry.Count(e => e.Status == ExpiryStatus.PorVencer),
                ExpiredLots: expiry.Count(e => e.Status == ExpiryStatus.Vencido),
                ClosuresWithDifference: await db.CashSessions.CountAsync(s => s.Difference != null && s.Difference != 0),
                SalesByCashier: last7);
        }).RequirePermission(Permission.Reportes).WithTags("Reportes")
            .WithSummary("Dashboard gerencial: ventas de hoy y de 7 días, alertas y cierres con diferencia (RF-073)");

        api.MapGet("/cash/closures", async (BusinessDb db) =>
            await db.CashSessions.Where(s => s.Status == "Cerrada").OrderByDescending(s => s.ClosedAt).Take(100)
                .ToListAsync()).RequirePermission(Permission.Reportes).WithTags("Reportes")
            .WithSummary("Últimos 100 cierres de caja consolidados (RF-060)");

    }

    /// <summary>Endpoints que solo invoca el Sync Service y el Notification Service; el Gateway no los expone.</summary>
    private static void MapInternal(WebApplication app)
    {
        var internalApi = app.MapGroup("/internal").AddEndpointFilter(async (context, next) =>
            context.HttpContext.HasInternalKey(app.Configuration) ? await next(context) : Results.Unauthorized());

        internalApi.MapPost("/events", async (SyncEnvelope message, EventApplier applier, CancellationToken ct) =>
        {
            try
            {
                await applier.ApplyAsync(message, ct);
                return Results.Ok();
            }
            catch (ArgumentException e)
            {
                return Results.Problem(e.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
            }
        });

        internalApi.MapGet("/catalog", async (long since, BusinessDb db) =>
        {
            var products = (await db.Products.ToListAsync()).Where(p => p.UpdatedAt.UtcTicks > since).ToList();
            var version = products.Count == 0 ? since : products.Max(p => p.UpdatedAt.UtcTicks);
            var settings = await OperationalSettings.GetAsync(db);
            return new PullResponse(version, products.Select(p => new CatalogProductDto(p.Id, p.Code, p.Name,
                p.ActiveIngredient, p.Concentration, p.Presentation, p.Laboratory, p.Category, p.RequiresPrescription,
                p.SalePrice, p.MinimumStock, p.Location, p.IsActive, p.UpdatedAt)).ToList(), settings.ExpiryThresholdDays);
        });

        // SCRUM-19 · HU-09: lotes creados en el servidor (recepción de compras) para que los puestos los vendan con FEFO.
        // Con snapshot=true (primera descarga del puesto) envía todos los lotes con su vencimiento y
        // stock oficiales, y la versión actual de la publicación para seguir desde ahí (SCRUM-112).
        internalApi.MapGet("/lots", async (long since, bool? snapshot, BusinessDb db) =>
        {
            if (snapshot == true)
            {
                var version = await db.LotFeed.MaxAsync(l => (long?)l.Sequence) ?? 0;
                var all = await db.Lots.OrderBy(l => l.LotNumber)
                    .Select(l => new LotChangeDto(version, l.Id, l.ProductId, l.LotNumber, l.ExpirationDate, l.Quantity))
                    .ToListAsync();
                return new LotPullResponse(version, all);
            }
            var lots = await db.LotFeed.Where(l => l.Sequence > since).OrderBy(l => l.Sequence).Take(500)
                .Select(l => new LotChangeDto(l.Sequence, l.LotId, l.ProductId, l.LotNumber, l.ExpirationDate,
                    l.Quantity))
                .ToListAsync();
            return new LotPullResponse(lots.Count == 0 ? since : lots[^1].Sequence, lots);
        });

        // SCRUM-130 · HU-28.1: frecuencia de alertas para el Notification Worker.
        internalApi.MapGet("/settings", async (BusinessDb db) => await OperationalSettings.GetForWorkerAsync(db));

        internalApi.MapGet("/alerts", async (BusinessDb db, TimeProvider time) =>
        {
            var today = Today(time);
            var products = await db.Products.ToListAsync();
            var lots = await db.Lots.ToListAsync();
            var settings = await OperationalSettings.GetAsync(db);
            var expiry = InventoryAlerts.Expiry(products, lots, today, settings.ExpiryThresholdDays).Select(e => new AlertDto(
                e.Status == ExpiryStatus.Vencido ? "Vencido" : "Por vencer", e.ProductName,
                $"Lote {e.LotNumber}: {e.Quantity} u., vence {e.ExpirationDate:dd/MM/yyyy} ({e.DaysToExpire} días)"));
            var low = InventoryAlerts.LowStock(products, lots, today).Select(a => new AlertDto("Stock mínimo",
                a.ProductName, $"Stock {a.Stock} u. (mínimo {a.MinimumStock})"));
            return expiry.Concat(low).ToList();
        });
    }

    private static async Task<List<SalesByCashierRow>> SalesByCashierAsync(BusinessDb db, DateOnly start, DateOnly end)
    {
        var from = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), LaPaz);
        var to = new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), LaPaz);
        var sales = await db.Sales.Where(s => s.Status == "Completada" && s.CreatedAt >= from && s.CreatedAt < to)
            .Select(s => new { s.CreatedAt, s.CashierName, s.Total }).ToListAsync();
        return sales
            .GroupBy(s => (Day: DateOnly.FromDateTime(s.CreatedAt.ToOffset(LaPaz).DateTime), s.CashierName))
            .Select(g => new SalesByCashierRow(g.Key.Day, g.Key.CashierName, g.Count(), g.Sum(s => s.Total)))
            .OrderBy(r => r.Day).ThenBy(r => r.CashierName)
            .ToList();
    }

    /// <summary>Bolivia usa UTC−4 todo el año (sin horario de verano).</summary>
    internal static readonly TimeSpan LaPaz = TimeSpan.FromHours(-4);

    internal static DateOnly Today(TimeProvider time) => DateOnly.FromDateTime(time.GetUtcNow().ToOffset(LaPaz).DateTime);
}
