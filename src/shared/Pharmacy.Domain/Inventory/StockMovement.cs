using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Inventory;

public enum StockMovementType
{
    Venta,
    Compra,
    Ajuste,
    Retiro,
}

/// <summary>Movimiento de inventario con usuario, fecha y motivo (RF-014 a RF-016, RF-019).</summary>
public class StockMovement
{
    private StockMovement() { }

    public StockMovement(Guid id, Guid productId, Guid lotId, StockMovementType type, int quantity,
        string reason, string userName, DateTimeOffset occurredAt)
    {
        if (quantity == 0) throw new DomainException("El movimiento debe tener una cantidad distinta de cero.");
        if (type is StockMovementType.Ajuste or StockMovementType.Retiro && string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Los ajustes y retiros requieren un motivo (RF-016).");

        Id = id;
        ProductId = productId;
        LotId = lotId;
        Type = type;
        Quantity = quantity;
        Reason = reason;
        UserName = userName;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid LotId { get; private set; }
    public StockMovementType Type { get; private set; }

    /// <summary>Positiva para entradas y negativa para salidas.</summary>
    public int Quantity { get; private set; }

    public string Reason { get; private set; } = "";
    public string UserName { get; private set; } = "";
    public DateTimeOffset OccurredAt { get; private set; }
}
