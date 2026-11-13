using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Purchasing;

namespace Pharmacy.Application.Demo;

/// <summary>
/// Proveedores y compra de demostración del Sprint 2: dos distribuidoras ficticias y una
/// compra pendiente de recibir para mostrar la recepción con lotes y diferencias.
/// </summary>
public static class DemoPurchasing
{
    public static readonly Guid DistribuidoraAndina = new("00000000-0000-0000-0003-000000000001");
    public static readonly Guid DrogueriaIllimani = new("00000000-0000-0000-0003-000000000002");
    public static readonly Guid PendingPurchase = new("00000000-0000-0000-0004-000000000001");

    public static (IReadOnlyList<Supplier> Suppliers, Purchase? Purchase) Create(
        IReadOnlyDictionary<Guid, Product> products, DateTimeOffset now)
    {
        var andina = new Supplier(DistribuidoraAndina, "1020304050", "Distribuidora Andina S.R.L.",
            "Carla Mamani", "2-2412345", "ventas@andina.example", "Av. 6 de Agosto 2100, La Paz", now);
        var illimani = new Supplier(DrogueriaIllimani, "2030405060", "Droguería Illimani Ltda.",
            "Jorge Quispe", "2-2798765", "pedidos@illimani.example", "Calle Comercio 845, La Paz", now);

        Purchase? purchase = null;
        var paracetamol = products.GetValueOrDefault(DemoCatalog.ProductId(1));
        var amoxicilina = products.GetValueOrDefault(DemoCatalog.ProductId(3));
        if (paracetamol is not null && amoxicilina is not null)
        {
            purchase = Purchase.Create(PendingPurchase, "OC-00001", andina, "operaciones", now);
            purchase.AddLine(paracetamol, 100, 0.30m);
            purchase.AddLine(amoxicilina, 50, 1.60m);
        }
        return ([andina, illimani], purchase);
    }
}
