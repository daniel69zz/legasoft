using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Inventory;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>
/// SCRUM-112 y SCRUM-35 · HU-22: el puesto alinea sus lotes con los del servidor; su stock es el del servidor
/// más los movimientos propios que todavía no llegaron a él.
/// </summary>
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

        var result = LotSync.Merge(new Dictionary<Guid, Lot> { [gel.Id] = gel }, new Dictionary<Guid, int>(),
            [FromServer(gel, -1, 6)]);

        Assert.Equal(1, result.Updated);
        Assert.True(gel.IsExpired(Today));   // el servidor lo tiene vencido: el puesto ya no lo vende
        Assert.Equal(6, gel.Quantity);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-35")]
    [Trait("Requirement", "RF-081")]
    public void Con_movimientos_pendientes_el_stock_es_el_del_servidor_menos_lo_que_falta_enviar()
    {
        var lot = Lot(Product(), "PCM-2401", 25, 100);

        // El servidor tiene 120; este puesto vendió 20 que todavía están en su Outbox.
        LotSync.Merge(new Dictionary<Guid, Lot> { [lot.Id] = lot }, new Dictionary<Guid, int> { [lot.Id] = -20 },
            [FromServer(lot, 9, 120)]);

        Assert.Equal(100, lot.Quantity);
        Assert.Equal(Today.AddDays(9), lot.ExpirationDate);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-35")]
    [Trait("Requirement", "RF-081")]
    public void Lo_que_vendio_otro_puesto_baja_el_stock_y_vale_el_ultimo_cambio_del_lote()
    {
        var lot = Lot(Product(), "PCM-2409", 200, 620);
        var change = FromServer(lot, 200, 0);

        // Dos cambios del mismo lote en una descarga: ventas de otro puesto (500) y luego otra (10).
        var result = LotSync.Merge(new Dictionary<Guid, Lot> { [lot.Id] = lot }, new Dictionary<Guid, int>(),
            [change with { Sequence = 7, Quantity = 120 }, change with { Sequence = 8, Quantity = 110 }]);

        Assert.Equal((1, 110), (result.Updated, lot.Quantity));
        Assert.Empty(result.Added);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-112")]
    [Trait("Requirement", "RF-033")]
    public void Agrega_los_lotes_nuevos_y_no_cuenta_los_que_ya_coinciden()
    {
        var product = Product();
        var same = Lot(product, "AMX-2405", 45, 30);
        var nuevo = new LotChangeDto(3, Guid.NewGuid(), product.Id, "AMX-2511", Today.AddDays(400), 90);

        var result = LotSync.Merge(new Dictionary<Guid, Lot> { [same.Id] = same }, new Dictionary<Guid, int>(),
            [FromServer(same, 45, 30), nuevo]);

        Assert.Equal(0, result.Updated);
        var added = Assert.Single(result.Added);
        Assert.Equal(("AMX-2511", 90), (added.LotNumber, added.Quantity));
    }
}
