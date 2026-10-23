using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>RF-095 (FEFO) y RF-024 (no vender vencidos).</summary>
public class FefoAllocatorTests
{
    [Fact]
    public void Asigna_primero_el_lote_que_vence_antes()
    {
        var p = Product();
        var late = Lot(p, "B", 300, 50);
        var early = Lot(p, "A", 20, 50);

        var result = FefoAllocator.Allocate([late, early], 10, Today);

        Assert.Single(result);
        Assert.Equal("A", result[0].Lot.LotNumber);
    }

    [Fact]
    public void Reparte_entre_lotes_cuando_el_primero_no_alcanza()
    {
        var p = Product();
        var early = Lot(p, "A", 20, 3);
        var late = Lot(p, "B", 300, 50);

        var result = FefoAllocator.Allocate([late, early], 5, Today);

        Assert.Equal([("A", 3), ("B", 2)], result.Select(a => (a.Lot.LotNumber, a.Quantity)));
    }

    [Fact]
    public void Nunca_asigna_lotes_vencidos()
    {
        var p = Product();
        var expired = Lot(p, "VENCIDO", -1, 100);
        var valid = Lot(p, "OK", 90, 5);

        var result = FefoAllocator.Allocate([expired, valid], 5, Today);

        Assert.All(result, a => Assert.Equal("OK", a.Lot.LotNumber));
    }

    [Fact]
    public void Un_lote_que_vence_hoy_ya_no_es_vendible()
    {
        var p = Product();
        Assert.Throws<DomainException>(() => FefoAllocator.Allocate([Lot(p, "HOY", 0, 10)], 1, Today));
    }

    [Fact]
    public void Rechaza_cuando_el_stock_vigente_no_alcanza()
    {
        var p = Product();
        var ex = Assert.Throws<DomainException>(() =>
            FefoAllocator.Allocate([Lot(p, "A", 10, 2), Lot(p, "V", -5, 50)], 3, Today));
        Assert.Contains("hay 2", ex.Message);
    }

    [Fact]
    public void Stock_vendible_excluye_vencidos()
    {
        var p = Product();
        Assert.Equal(7, FefoAllocator.SellableStock([Lot(p, "A", 10, 7), Lot(p, "V", -1, 40)], Today));
    }

    [Fact]
    public void Withdraw_de_un_lote_vencido_falla()
    {
        var lot = Lot(Product(), "V", -3, 10);
        Assert.Throws<DomainException>(() => lot.Withdraw(1, Today));
    }
}
