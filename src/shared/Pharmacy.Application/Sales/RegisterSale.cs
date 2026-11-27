using FluentValidation;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;

namespace Pharmacy.Application.Sales;

public sealed record SaleItemRequest(Guid ProductId, int Quantity);

public sealed record RegisterSaleCommand(
    IReadOnlyList<SaleItemRequest> Items,
    decimal CashReceived,
    string? PrescriptionNumber = null,
    decimal Discount = 0,
    string? CustomerName = null,
    string? CustomerTaxId = null,
    string? PrescriptionDoctor = null,
    string? PrescriptionPatient = null,
    DateOnly? PrescriptionIssuedOn = null);

public sealed record SaleReceipt(Guid SaleId, string ReceiptNumber, DateTimeOffset CreatedAt, string CashierName,
    IReadOnlyList<SaleLinePayload> Lines, decimal Subtotal, decimal Discount, decimal Total, decimal CashReceived,
    decimal Change, string CustomerName = "", string CustomerTaxId = "", string PrescriptionNumber = "",
    string Status = "Completada");

public sealed class RegisterSaleValidator : AbstractValidator<RegisterSaleCommand>
{
    public RegisterSaleValidator()
    {
        RuleFor(c => c.Items).NotEmpty().WithMessage("Agregue al menos un producto.");
        RuleForEach(c => c.Items).Must(i => i.Quantity > 0).WithMessage("Las cantidades deben ser mayores a cero.");
        RuleFor(c => c.CashReceived).GreaterThan(0).WithMessage("Registre el efectivo recibido.");
        RuleFor(c => c.Discount).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Caso de uso SalesLocalService (Documento 3, C3 Desktop): aplica FEFO, bloquea
/// lotes vencidos, verifica la receta, registra el efectivo y descuenta el stock.
/// Venta, detalle, stock, caja y Outbox se confirman en una única transacción.
/// </summary>
public sealed class RegisterSaleHandler(
    ICatalogRepository catalog,
    ISalesRepository sales,
    ICashRepository cash,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser user,
    IOperationalSettings? settings = null)
{
    private readonly RegisterSaleValidator _validator = new();

    public async Task<SaleReceipt> HandleAsync(RegisterSaleCommand command, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(command, ct);

        var session = await cash.GetOpenAsync(user.DeviceId, ct)
            ?? throw new DomainException("Abra la caja antes de registrar ventas (RF-052).");

        var items = command.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new SaleItemRequest(g.Key, g.Sum(i => i.Quantity)))
            .ToList();
        var ids = items.Select(i => i.ProductId).ToList();
        var products = (await catalog.GetProductsAsync(ids, ct)).ToDictionary(p => p.Id);
        var lots = (await catalog.GetLotsAsync(ids, ct)).ToLookup(l => l.ProductId);

        var now = clock.Now;
        var today = clock.Today;
        var number = await sales.CountByDeviceAsync(user.DeviceId, ct) + 1;
        var sale = Sale.Start(Guid.NewGuid(), $"{user.DeviceId}-{number:D6}", user.DeviceId, user.UserName,
            session.Id, now);

        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new DomainException("Uno de los productos ya no existe en el catálogo.");
            sale.AddLine(product, FefoAllocator.Allocate(lots[item.ProductId], item.Quantity, today));
        }

        // SCRUM-30 · HU-27 (RF-096): solo con permiso y hasta el máximo configurado.
        if (command.Discount > 0)
        {
            if (!RolePermissions.Has(user.Role, Permission.Descuentos))
                throw new DomainException("No tiene permiso para aplicar descuentos (RF-096).");
            var maxPercent = settings is null ? DiscountPolicy.DefaultMaxPercent
                : await settings.GetMaxDiscountPercentAsync(ct);
            DiscountPolicy.EnsureWithinLimit(command.Discount, sale.Subtotal, maxPercent);
            sale.ApplyDiscount(command.Discount, user.UserName);
        }
        sale.SetCustomer(command.CustomerName ?? "", command.CustomerTaxId ?? "");
        // SCRUM-29 · HU-25: la receta solo se registra si la venta la requiere.
        var prescription = sale.RequiresPrescription
            ? Prescription.Create(command.PrescriptionNumber, command.PrescriptionDoctor, command.PrescriptionPatient,
                command.PrescriptionIssuedOn, today)
            : null;
        sale.Complete(command.CashReceived, prescription);

        var lotsById = lots.SelectMany(g => g).ToDictionary(l => l.Id);
        foreach (var line in sale.Lines)
        {
            lotsById[line.LotId].Withdraw(line.Quantity, today);
            catalog.AddMovement(new StockMovement(Guid.NewGuid(), line.ProductId, line.LotId,
                StockMovementType.Venta, -line.Quantity, $"Venta {sale.ReceiptNumber}", user.UserName, now));
        }

        session.RegisterSale(sale.Id, sale.Total, user.UserName, now);
        sales.Add(sale);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Sales, EventTypes.SaleCreated, sale.Id,
            SalePayloads.Created(sale), now));

        await unitOfWork.SaveChangesAsync(ct);
        return SalePayloads.Receipt(sale);
    }
}

public static class SalePayloads
{
    public static IReadOnlyList<SaleLinePayload> Lines(Sale sale) => sale.Lines
        .Select(l => new SaleLinePayload(l.ProductId, l.ProductName, l.LotId, l.LotNumber, l.Quantity, l.UnitPrice))
        .ToList();

    public static SaleCreatedPayload Created(Sale sale) => new(sale.Id, sale.ReceiptNumber, sale.DeviceId,
        sale.CashierName, sale.CashSessionId, sale.CreatedAt, sale.Subtotal, sale.Discount, sale.Total,
        sale.CashReceived, sale.Change, sale.PrescriptionNumber, sale.CustomerName, sale.CustomerTaxId, Lines(sale),
        sale.PrescriptionDoctor, sale.PrescriptionPatient, sale.PrescriptionIssuedOn, sale.DiscountAuthorizedBy);

    public static SaleReceipt Receipt(Sale sale) => new(sale.Id, sale.ReceiptNumber, sale.CreatedAt,
        sale.CashierName, Lines(sale), sale.Subtotal, sale.Discount, sale.Total, sale.CashReceived, sale.Change,
        sale.CustomerName, sale.CustomerTaxId, sale.PrescriptionNumber, sale.Status.ToString());
}
