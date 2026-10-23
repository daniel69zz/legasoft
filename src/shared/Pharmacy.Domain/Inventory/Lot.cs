using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Inventory;

/// <summary>Lote de un producto con su fecha de vencimiento y existencia (RF-020, RF-021).</summary>
public class Lot
{
    private Lot() { }

    public Lot(Guid id, Guid productId, string lotNumber, DateOnly expirationDate, int quantity)
    {
        if (string.IsNullOrWhiteSpace(lotNumber)) throw new DomainException("El número de lote es obligatorio.");
        if (quantity < 0) throw new DomainException("La cantidad del lote no puede ser negativa.");

        Id = id;
        ProductId = productId;
        LotNumber = lotNumber.Trim();
        ExpirationDate = expirationDate;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string LotNumber { get; private set; } = "";
    public DateOnly ExpirationDate { get; private set; }
    public int Quantity { get; private set; }

    /// <summary>RF-023: un lote está vencido desde el mismo día de su fecha de vencimiento.</summary>
    public bool IsExpired(DateOnly today) => ExpirationDate <= today;

    public int DaysToExpire(DateOnly today) => ExpirationDate.DayNumber - today.DayNumber;

    /// <summary>Salida por venta. RF-024: nunca se vende un lote vencido.</summary>
    public void Withdraw(int quantity, DateOnly today)
    {
        if (IsExpired(today))
            throw new DomainException($"El lote {LotNumber} está vencido y no se puede vender.");
        Decrease(quantity);
    }

    /// <summary>Salida sin validar vencimiento: retiro de vencidos o consolidación en el servidor.</summary>
    public void Decrease(int quantity)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor a cero.");
        if (quantity > Quantity)
            throw new DomainException($"El lote {LotNumber} solo tiene {Quantity} unidades.");
        Quantity -= quantity;
    }

    public void Increase(int quantity)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor a cero.");
        Quantity += quantity;
    }
}
