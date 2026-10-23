using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Sales;

public sealed record VoidSaleCommand(Guid SaleId, string Reason);

/// <summary>RF-048: anula una venta con motivo, devuelve el stock a sus lotes y registra la salida de caja.</summary>
public sealed class VoidSaleHandler(
    ICatalogRepository catalog,
    ISalesRepository sales,
    ICashRepository cash,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser user)
{
    public async Task HandleAsync(VoidSaleCommand command, CancellationToken ct = default)
    {
        var sale = await sales.GetAsync(command.SaleId, ct) ?? throw new DomainException("La venta no existe.");
        var session = await cash.GetOpenAsync(user.DeviceId, ct)
            ?? throw new DomainException("Abra la caja para registrar la anulación.");

        var now = clock.Now;
        sale.Void(command.Reason, user.UserName, now);

        var lots = (await catalog.GetLotsAsync(sale.Lines.Select(l => l.ProductId).Distinct().ToList(), ct))
            .ToDictionary(l => l.Id);
        foreach (var line in sale.Lines)
        {
            lots[line.LotId].Increase(line.Quantity);
            catalog.AddMovement(new StockMovement(Guid.NewGuid(), line.ProductId, line.LotId,
                StockMovementType.Ajuste, line.Quantity, $"Anulación {sale.ReceiptNumber}: {command.Reason}",
                user.UserName, now));
        }

        session.RegisterSaleVoid(sale.Id, sale.Total, command.Reason, user.UserName, now);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Sales, EventTypes.SaleVoided, sale.Id,
            new SaleVoidedPayload(sale.Id, sale.VoidReason, user.UserName, now), now, version: 2));

        await unitOfWork.SaveChangesAsync(ct);
    }
}
