using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Purchasing;

/// <summary>
/// Precio de compra que un proveedor ofrece por un producto (HU-10.1, RF-034). Cada cambio es un
/// registro nuevo: el vigente es el más reciente y los anteriores forman el historial (RF-035).
/// </summary>
public class SupplierPrice
{
    private SupplierPrice() { }

    public SupplierPrice(Guid id, Guid supplierId, Guid productId, decimal price, string registeredBy,
        DateTimeOffset registeredAt)
    {
        if (price <= 0) throw new DomainException("El precio ofrecido debe ser mayor a cero.");
        Id = id;
        SupplierId = supplierId;
        ProductId = productId;
        Price = MoneyMath.Round(price);
        RegisteredBy = registeredBy;
        RegisteredAt = registeredAt;
    }

    public Guid Id { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public string RegisteredBy { get; private set; } = "";
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Precio vigente de cada proveedor y producto: el registro más reciente.</summary>
    public static IEnumerable<SupplierPrice> Current(IEnumerable<SupplierPrice> prices) =>
        prices.GroupBy(p => (p.SupplierId, p.ProductId))
            .Select(g => g.OrderByDescending(p => p.RegisteredAt).First());

    /// <summary>HU-10.2 (RF-036): precios vigentes de un producto entre los proveedores activos, del menor al mayor.</summary>
    public static IReadOnlyList<SupplierPrice> Compare(IEnumerable<SupplierPrice> prices, Guid productId,
        IReadOnlySet<Guid> activeSuppliers) =>
        Current(prices.Where(p => p.ProductId == productId))
            .Where(p => activeSuppliers.Contains(p.SupplierId))
            .OrderBy(p => p.Price)
            .ThenBy(p => p.RegisteredAt)
            .ToList();
}
