using FluentValidation;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Inventory;

/// <summary>
/// Movimiento manual sobre un lote. Para entradas y salidas la cantidad son las unidades
/// (siempre positiva); para un ajuste es la diferencia con signo (−3 resta tres unidades).
/// </summary>
public sealed record AdjustStockCommand(Guid ProductId, Guid LotId, StockMovementType Type, int Quantity,
    string Reason);

public sealed record StockMovementRow(DateTimeOffset OccurredAt, string Type, string LotNumber, int Quantity,
    string Reason, string UserName);

public sealed class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(c => c.Type).Must(StockMovement.IsManual)
            .WithMessage("Solo se registran a mano entradas, salidas y ajustes.");
        RuleFor(c => c.Quantity).NotEqual(0).WithMessage("Indique la cantidad del movimiento.");
        RuleFor(c => c.Quantity).GreaterThan(0)
            .When(c => c.Type is StockMovementType.Entrada or StockMovementType.Salida or StockMovementType.Retiro)
            .WithMessage("En entradas, salidas y retiros indique las unidades como un número positivo.");
        RuleFor(c => c.Reason).NotEmpty().WithMessage("Indique el motivo del movimiento (RF-016).");
    }
}

/// <summary>
/// SCRUM-101 · HU-06.3 (RF-014 a RF-016) y SCRUM-121 · HU-07.2 (RF-025): entrada, salida, ajuste o retiro manual de un lote en el puesto. El
/// movimiento guarda usuario, fecha, hora y motivo, y viaja al servidor por el Outbox
/// (StockMovementCreated) en la misma transacción que el cambio de stock (ADR-003).
/// </summary>
public sealed class AdjustStockHandler(
    ICatalogRepository catalog,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser user)
{
    private readonly AdjustStockValidator _validator = new();

    public async Task<StockMovementRow> HandleAsync(AdjustStockCommand command, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(command, ct);

        var lot = (await catalog.GetLotsAsync([command.ProductId], ct)).SingleOrDefault(l => l.Id == command.LotId)
            ?? throw new DomainException("El lote no existe para ese producto.");

        // SCRUM-121 · HU-07.2 (RF-025): el retiro, igual que la salida, resta unidades; acepta lotes vencidos.
        var quantity = command.Type switch
        {
            StockMovementType.Salida or StockMovementType.Retiro => -command.Quantity,
            _ => command.Quantity,
        };
        if (quantity > 0) lot.Increase(quantity);
        else lot.Decrease(-quantity);

        var now = clock.Now;
        var movement = new StockMovement(Guid.NewGuid(), command.ProductId, lot.Id, command.Type, quantity,
            command.Reason.Trim(), user.UserName, now);
        catalog.AddMovement(movement);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Inventory, EventTypes.StockMovementCreated,
            movement.Id, new StockMovementPayload(movement.Id, movement.ProductId, lot.Id, lot.LotNumber,
                movement.Type.ToString(), movement.Quantity, movement.Reason, movement.UserName, now), now));

        await unitOfWork.SaveChangesAsync(ct);
        return new StockMovementRow(now, movement.Type.ToString(), lot.LotNumber, quantity, movement.Reason,
            movement.UserName);
    }
}
