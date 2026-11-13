using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Domain.Purchasing;

public enum PurchaseStatus
{
    Pendiente,
    Recibida,
}

/// <summary>Lo que se registra al recibir una línea: lote, vencimiento, unidades en buen estado y dañadas.</summary>
public sealed record ReceptionItem(Guid LineId, string LotNumber, DateOnly? ExpirationDate, int ReceivedQuantity,
    int DamagedQuantity);

/// <summary>Lote creado por la recepción de una línea de compra.</summary>
public sealed record ReceivedLot(PurchaseLine Line, Lot Lot);

/// <summary>
/// Compra a un proveedor y su recepción (SCRUM-19 · HU-09, RF-030 a RF-033, RF-020, RF-107 y RF-108).
/// Una compra se recibe una sola vez: lo que no llegó queda registrado como faltante.
/// </summary>
public class Purchase
{
    private readonly List<PurchaseLine> _lines = [];

    private Purchase() { }

    public static Purchase Create(Guid id, string number, Supplier supplier, string createdBy, DateTimeOffset now)
    {
        if (!supplier.IsActive) throw new DomainException($"El proveedor {supplier.Name} está desactivado.");
        return new Purchase
        {
            Id = id,
            Number = number,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            CreatedBy = createdBy,
            CreatedAt = now,
            Status = PurchaseStatus.Pendiente,
        };
    }

    public Guid Id { get; private set; }
    public string Number { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = "";
    public string CreatedBy { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public string ReceivedBy { get; private set; } = "";
    public DateTimeOffset? ReceivedAt { get; private set; }

    /// <summary>Líneas en el orden en que se registraron (la base de datos no garantiza el orden).</summary>
    public IReadOnlyList<PurchaseLine> Lines => _lines.OrderBy(l => l.Position).ToList();

    /// <summary>RF-031: total calculado a partir de las cantidades pedidas y el costo unitario.</summary>
    public decimal Total => MoneyMath.Round(_lines.Sum(l => l.Subtotal));

    /// <summary>Costo de lo que ingresó en buen estado.</summary>
    public decimal ReceivedTotal => MoneyMath.Round(_lines.Sum(l => l.UnitCost * l.ReceivedQuantity));

    /// <summary>RF-030: agrega un producto con su cantidad y costo unitario.</summary>
    public void AddLine(Product product, int quantity, decimal unitCost)
    {
        if (Status != PurchaseStatus.Pendiente) throw new DomainException("La compra ya fue recibida.");
        if (quantity <= 0) throw new DomainException($"La cantidad de {product.Name} debe ser mayor a cero.");
        if (unitCost <= 0) throw new DomainException($"El costo unitario de {product.Name} debe ser mayor a cero.");
        if (_lines.Any(l => l.ProductId == product.Id))
            throw new DomainException($"{product.Name} ya está en la compra.");

        _lines.Add(new PurchaseLine(Guid.NewGuid(), Id, _lines.Count + 1, product.Id, product.DisplayName, quantity,
            MoneyMath.Round(unitCost)));
    }

    /// <summary>
    /// RF-032, RF-107 y RF-108: registra la recepción. Por cada línea con unidades en buen estado
    /// crea un lote con su número y vencimiento (RF-020); las unidades dañadas y faltantes no
    /// ingresan al stock. Las líneas sin datos de recepción se consideran no recibidas.
    /// </summary>
    public IReadOnlyList<ReceivedLot> Receive(IReadOnlyCollection<ReceptionItem> items, string userName,
        DateTimeOffset now, DateOnly today)
    {
        if (Status != PurchaseStatus.Pendiente) throw new DomainException($"La compra {Number} ya fue recibida.");
        if (_lines.Count == 0) throw new DomainException("La compra no tiene productos.");

        var byLine = items.ToDictionary(i => i.LineId);
        if (byLine.Keys.Any(id => _lines.All(l => l.Id != id)))
            throw new DomainException("La recepción incluye una línea que no pertenece a la compra.");
        if (items.All(i => i.ReceivedQuantity == 0 && i.DamagedQuantity == 0))
            throw new DomainException("La recepción no registra ninguna unidad.");

        var lots = new List<ReceivedLot>();
        foreach (var line in Lines)
        {
            var item = byLine.GetValueOrDefault(line.Id) ?? new ReceptionItem(line.Id, "", null, 0, 0);
            var lot = line.Receive(item, today);
            if (lot is not null) lots.Add(new ReceivedLot(line, lot));
        }

        Status = PurchaseStatus.Recibida;
        ReceivedBy = userName;
        ReceivedAt = now;
        return lots;
    }
}

/// <summary>Producto pedido y, tras la recepción, lo que llegó realmente.</summary>
public class PurchaseLine
{
    private PurchaseLine() { }

    internal PurchaseLine(Guid id, Guid purchaseId, int position, Guid productId, string productName, int quantity,
        decimal unitCost)
    {
        Id = id;
        PurchaseId = purchaseId;
        Position = position;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    public Guid Id { get; private set; }
    public Guid PurchaseId { get; private set; }
    public int Position { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public int DamagedQuantity { get; private set; }
    public string LotNumber { get; private set; } = "";
    public DateOnly? ExpirationDate { get; private set; }
    public Guid? LotId { get; private set; }

    public decimal Subtotal => MoneyMath.Round(UnitCost * Quantity);

    /// <summary>Unidades que no llegaron.</summary>
    public int MissingQuantity => Quantity - ReceivedQuantity - DamagedQuantity;

    /// <summary>RF-107: diferencia entre lo pedido y lo que ingresó en buen estado.</summary>
    public int Difference => Quantity - ReceivedQuantity;

    internal Lot? Receive(ReceptionItem item, DateOnly today)
    {
        if (item.ReceivedQuantity < 0 || item.DamagedQuantity < 0)
            throw new DomainException($"{ProductName}: las cantidades recibidas no pueden ser negativas.");
        if (item.ReceivedQuantity + item.DamagedQuantity > Quantity)
            throw new DomainException(
                $"{ProductName}: se registraron {item.ReceivedQuantity + item.DamagedQuantity} unidades y se pidieron {Quantity}.");

        ReceivedQuantity = item.ReceivedQuantity;
        DamagedQuantity = item.DamagedQuantity;
        if (item.ReceivedQuantity == 0) return null;

        if (string.IsNullOrWhiteSpace(item.LotNumber))
            throw new DomainException($"{ProductName}: registre el número de lote (RF-020).");
        if (item.ExpirationDate is not { } expiration)
            throw new DomainException($"{ProductName}: registre la fecha de vencimiento del lote (RF-020).");
        if (expiration <= today)
            throw new DomainException($"{ProductName}: el lote {item.LotNumber.Trim()} ya está vencido y no puede ingresar.");

        var lot = new Lot(Guid.NewGuid(), ProductId, item.LotNumber, expiration, item.ReceivedQuantity);
        LotNumber = lot.LotNumber;
        ExpirationDate = expiration;
        LotId = lot.Id;
        return lot;
    }
}
