using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Inventory;

public enum StockMovementType
{
    Venta,
    Compra,
    Ajuste,
    Retiro,

    /// <summary>Entrada manual a un lote existente (RF-014).</summary>
    Entrada,

    /// <summary>Salida manual de un lote (RF-015).</summary>
    Salida,
}

/// <summary>Movimiento de inventario con usuario, fecha y motivo (RF-014 a RF-016, RF-019).</summary>
public class StockMovement
{
    private StockMovement() { }

    public StockMovement(Guid id, Guid productId, Guid lotId, StockMovementType type, int quantity,
        string reason, string userName, DateTimeOffset occurredAt)
    {
        if (quantity == 0) throw new DomainException("El movimiento debe tener una cantidad distinta de cero.");
        if (IsManual(type) && string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Los movimientos manuales requieren un motivo (RF-016).");
        if (type == StockMovementType.Entrada && quantity < 0)
            throw new DomainException("Una entrada debe sumar unidades.");
        if (type is StockMovementType.Salida or StockMovementType.Retiro && quantity > 0)
            throw new DomainException("Una salida o un retiro debe restar unidades.");

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

    /// <summary>Movimientos que registra una persona y no una venta o compra: exigen motivo.</summary>
    public static bool IsManual(StockMovementType type) =>
        type is StockMovementType.Ajuste or StockMovementType.Retiro or StockMovementType.Entrada
            or StockMovementType.Salida;
}
