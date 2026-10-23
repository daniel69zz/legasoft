using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Sales;

/// <summary>Línea de venta asociada a un lote concreto, para trazar FEFO y vencimientos.</summary>
public class SaleLine
{
    private SaleLine() { }

    internal SaleLine(Guid id, Guid saleId, Guid productId, string productName, Guid lotId, string lotNumber,
        int quantity, decimal unitPrice, bool requiresPrescription)
    {
        Id = id;
        SaleId = saleId;
        ProductId = productId;
        ProductName = productName;
        LotId = lotId;
        LotNumber = lotNumber;
        Quantity = quantity;
        UnitPrice = unitPrice;
        RequiresPrescription = requiresPrescription;
    }

    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public Guid LotId { get; private set; }
    public string LotNumber { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public bool RequiresPrescription { get; private set; }

    public decimal Subtotal => MoneyMath.Round(UnitPrice * Quantity);
}
