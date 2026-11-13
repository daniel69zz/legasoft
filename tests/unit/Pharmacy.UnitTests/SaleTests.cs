using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>RF-041, RF-044, RF-045, RF-048, RF-091 y RF-096.</summary>
public class SaleTests
{
    private static Sale NewSale() => Sale.Start(Guid.NewGuid(), "PUESTO-01-000001", "PUESTO-01", "cajero1",
        Guid.NewGuid(), Now);

    [Fact]
    [Trait("JiraIssue", "SCRUM-62")]
    [Trait("Requirement", "RF-045")]
    public void Calcula_total_y_cambio()
    {
        var p = Product(price: 2.50m);
        var sale = NewSale();
        sale.AddLine(p, FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 3, Today));

        sale.Complete(10m, null);

        Assert.Equal(7.50m, sale.Total);
        Assert.Equal(2.50m, sale.Change);
        Assert.Equal(SaleStatus.Completada, sale.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-63")]
    [Trait("Requirement", "RF-091")]
    public void Exige_receta_para_medicamentos_que_la_requieren()
    {
        var p = Product(prescription: true);
        var sale = NewSale();
        sale.AddLine(p, FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 1, Today));

        Assert.Throws<DomainException>(() => sale.Complete(10m, " "));
        sale.Complete(10m, "RX-123");
        Assert.Equal("RX-123", sale.PrescriptionNumber);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-62")]
    [Trait("Requirement", "RF-044")]
    public void Rechaza_efectivo_insuficiente()
    {
        var p = Product(price: 45m);
        var sale = NewSale();
        sale.AddLine(p, FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 1, Today));

        Assert.Throws<DomainException>(() => sale.Complete(40m, null));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-30")]
    [Trait("Requirement", "RF-096")]
    public void Descuento_autorizado_reduce_el_total_y_no_supera_el_subtotal()
    {
        var p = Product(price: 10m);
        var sale = NewSale();
        sale.AddLine(p, FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 2, Today));

        Assert.Throws<DomainException>(() => sale.ApplyDiscount(25m, "admin"));
        sale.ApplyDiscount(5m, "admin");
        Assert.Equal(15m, sale.Total);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-64")]
    [Trait("Requirement", "RF-048")]
    public void Anular_exige_motivo()
    {
        var p = Product();
        var sale = NewSale();
        sale.AddLine(p, FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 1, Today));
        sale.Complete(5m, null);

        Assert.Throws<DomainException>(() => sale.Void("", "admin", Now));
        sale.Void("Error de cobro", "admin", Now);
        Assert.Equal(SaleStatus.Anulada, sale.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-011")]
    public void No_se_vende_un_producto_desactivado()
    {
        var p = Product();
        var lots = FefoAllocator.Allocate([Lot(p, "A", 50, 10)], 1, Today);
        p.SetActive(false, Now);

        Assert.Throws<DomainException>(() => NewSale().AddLine(p, lots));
    }
}
