using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Inventory;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>SCRUM-112: el puesto alinea sus lotes con los del servidor sin pisar su stock movido.</summary>
public class LotSyncTests
{
    private static LotChangeDto FromServer(Lot lot, int daysToExpire, int quantity) =>
        new(0, lot.Id, lot.ProductId, lot.LotNumber, Today.AddDays(daysToExpire), quantity);

    [Fact]
    [Trait("JiraIssue", "SCRUM-112")]
    [Trait("Requirement", "RF-024")]
    public void Un_lote_sin_movimientos_toma_vencimiento_y_stock_del_servidor()
    {
        var gel = Lot(Product(), "GEL-2404", 15, 8);

        var result = LotSync.Merge(new Dictionary<Guid, Lot> { [gel.Id] = gel }, new HashSet<Guid>(),
            [FromServer(gel, -1, 6)]);

        Assert.Equal(1, result.Updated);
        Assert.True(gel.IsExpired(Today));   // el servidor lo tiene vencido: el puesto ya no lo vende
        Assert.Equal(6, gel.Quantity);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-112")]
    [Trait("Requirement", "RF-024")]
    public void Si_el_puesto_movio_el_lote_conserva_su_stock_y_corrige_el_vencimiento()
    {
        var lot = Lot(Product(), "PCM-2401", 25, 100);

        LotSync.Merge(new Dictionary<Guid, Lot> { [lot.Id] = lot }, new HashSet<Guid> { lot.Id },
            [FromServer(lot, 9, 120)]);

        Assert.Equal(100, lot.Quantity);
        Assert.Equal(Today.AddDays(9), lot.ExpirationDate);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-112")]
    [Trait("Requirement", "RF-033")]
    public void Agrega_los_lotes_nuevos_y_no_cuenta_los_que_ya_coinciden()
    {
        var product = Product();
        var same = Lot(product, "AMX-2405", 45, 30);
        var nuevo = new LotChangeDto(3, Guid.NewGuid(), product.Id, "AMX-2511", Today.AddDays(400), 90);

        var result = LotSync.Merge(new Dictionary<Guid, Lot> { [same.Id] = same }, new HashSet<Guid>(),
            [FromServer(same, 45, 30), nuevo]);

        Assert.Equal(0, result.Updated);
        var added = Assert.Single(result.Added);
        Assert.Equal(("AMX-2511", 90), (added.LotNumber, added.Quantity));
    }
}
