namespace Pharmacy.Domain.Cash;

public enum CashMovementType
{
    Venta,
    Egreso,
    AnulacionVenta,
}

/// <summary>Ingreso o egreso de caja con motivo, categoría y responsable (RF-053, RF-055).</summary>
public class CashMovement
{
    private CashMovement() { }

    internal CashMovement(Guid id, Guid cashSessionId, CashMovementType type, decimal amount, string reason,
        string category, string userName, DateTimeOffset occurredAt, Guid? saleId)
    {
        Id = id;
        CashSessionId = cashSessionId;
        Type = type;
        Amount = amount;
        Reason = reason;
        Category = category;
        UserName = userName;
        OccurredAt = occurredAt;
        SaleId = saleId;
    }

    public Guid Id { get; private set; }
    public Guid CashSessionId { get; private set; }
    public CashMovementType Type { get; private set; }

    /// <summary>Siempre positivo; el signo lo da el tipo de movimiento.</summary>
    public decimal Amount { get; private set; }

    public string Reason { get; private set; } = "";
    public string Category { get; private set; } = "";
    public string UserName { get; private set; } = "";
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? SaleId { get; private set; }

    public decimal SignedAmount => Type == CashMovementType.Venta ? Amount : -Amount;
}
