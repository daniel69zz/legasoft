using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

/// <summary>SCRUM-28 · HU-24 (RF-084 a RF-087): clientes e historial de compras.</summary>
public static partial class Sprint3Endpoints
{
    private static void MapCustomers(RouteGroupBuilder api)
    {
        var customers = api.MapGroup("/customers").WithTags("Clientes");

        customers.MapGet("", async (string? search, BusinessDb db) =>
            {
                var all = await db.Customers.OrderBy(c => c.Name).ToListAsync();
                var term = search?.Trim() ?? "";
                return all.Where(c => term.Length == 0
                        || c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || c.TaxId.Contains(term))
                    .Take(100).Select(ToDto).ToList();
            })
            .RequireAnyPermission(Permission.Clientes, Permission.Ventas)
            .WithSummary("[SCRUM-28 · HU-24] Busca clientes por nombre o NIT/CI (RF-086)")
            .Produces<List<CustomerDto>>();

        customers.MapPost("", async (CustomerRequest request, BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                Validate(request);
                var taxId = request.TaxId.Trim();
                if (await db.Customers.SingleOrDefaultAsync(c => c.TaxId == taxId) is { } existing)
                    return Results.Conflict(ToDto(existing));   // RF-084: se muestra el cliente existente
                var customer = new CustomerRecord
                {
                    Id = Guid.NewGuid(), Name = request.Name.Trim(), TaxId = taxId, Phone = request.Phone.Trim(),
                    Email = request.Email.Trim(), CreatedAt = time.GetUtcNow(), CreatedBy = UserName(http),
                };
                db.Customers.Add(customer);
                Audit(db, http, time, "Registro de cliente", $"{customer.Name} · NIT/CI {customer.TaxId}");
                await db.SaveChangesAsync();
                return Results.Created($"/api/customers/{customer.Id}", ToDto(customer));
            })
            .RequirePermission(Permission.Clientes)
            .WithSummary("[SCRUM-28 · HU-24] Registra un cliente; con un NIT/CI ya registrado responde 409 con el existente (RF-084)")
            .Produces<CustomerDto>(StatusCodes.Status201Created).Produces<CustomerDto>(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        customers.MapPut("/{id:guid}", async (Guid id, CustomerRequest request, BusinessDb db, HttpContext http,
                TimeProvider time) =>
            {
                Validate(request);
                var customer = await db.Customers.FindAsync(id);
                if (customer is null) return Results.NotFound();
                var taxId = request.TaxId.Trim();
                if (await db.Customers.AnyAsync(c => c.TaxId == taxId && c.Id != id))
                    throw new DomainException($"Ya existe otro cliente con el NIT/CI {taxId}.");
                customer.Name = request.Name.Trim();
                customer.TaxId = taxId;
                customer.Phone = request.Phone.Trim();
                customer.Email = request.Email.Trim();
                Audit(db, http, time, "Modificación de cliente", $"{customer.Name} · NIT/CI {customer.TaxId}");
                await db.SaveChangesAsync();
                return Results.Ok(ToDto(customer));
            })
            .RequirePermission(Permission.Clientes)
            .WithSummary("[SCRUM-28 · HU-24] Modifica un cliente (RF-085)")
            .Produces<CustomerDto>().ProducesProblem(StatusCodes.Status404NotFound);

        customers.MapGet("/{id:guid}/purchases", async (Guid id, BusinessDb db) =>
            {
                var customer = await db.Customers.FindAsync(id);
                if (customer is null) return Results.NotFound();
                var sales = (await db.Sales.Include(s => s.Lines).Where(s => s.CustomerTaxId == customer.TaxId)
                    .ToListAsync()).OrderByDescending(s => s.CreatedAt);
                return Results.Ok(sales.Select(s => new CustomerPurchaseDto(s.CreatedAt, s.ReceiptNumber, s.Status,
                    s.Total, s.Lines.Select(l => $"{l.ProductName} × {l.Quantity}").ToList())).ToList());
            })
            .RequireAnyPermission(Permission.Clientes, Permission.Ventas)
            .WithSummary("[SCRUM-28 · HU-24] Historial de compras del cliente con fecha, productos y total (RF-087)")
            .Produces<List<CustomerPurchaseDto>>().ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static CustomerDto ToDto(CustomerRecord c) => new(c.Id, c.Name, c.TaxId, c.Phone, c.Email, c.CreatedAt);

    private static void Validate(CustomerRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) throw new DomainException("Indique el nombre del cliente.");
        var taxId = r.TaxId?.Trim() ?? "";
        if (taxId.Length is < 5 or > 15 || !taxId.All(char.IsDigit))
            throw new DomainException("El NIT o CI debe tener entre 5 y 15 dígitos.");
        if (r.Email.Length > 0 && !r.Email.Contains('@')) throw new DomainException("El correo electrónico no es válido.");
    }
}
