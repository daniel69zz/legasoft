using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Purchasing;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>HU-08 y HU-09: reglas de proveedores, compras y recepción en Pharmacy.Domain.</summary>
public class PurchaseTests
{
    private static Supplier Supplier() =>
        new(Guid.NewGuid(), "1020304050", "Distribuidora Andina", "Carla", "2-2412345", "", "", Now);

    private static (Purchase Purchase, PurchaseLine Line) PurchaseOf(int quantity, decimal unitCost = 1.50m)
    {
        var purchase = Purchase.Create(Guid.NewGuid(), "OC-00001", Supplier(), "operaciones", Now);
        purchase.AddLine(Product(), quantity, unitCost);
        return (purchase, purchase.Lines.Single());
    }

    [Theory]
    [InlineData("", "Andina", "222", "NIT")]
    [InlineData("123", "", "222", "razón social")]
    [InlineData("123", "Andina", "", "teléfono")]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-026")]
    public void Un_proveedor_sin_datos_obligatorios_se_rechaza_indicando_el_dato(string taxId, string name,
        string phone, string missing)
    {
        var error = Assert.Throws<DomainException>(() =>
            new Supplier(Guid.NewGuid(), taxId, name, "", phone, "", "", Now));
        Assert.Contains(missing, error.Message);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-18")]
    [Trait("Requirement", "RF-027")]
    public void Modificar_un_proveedor_conserva_su_NIT()
    {
        var supplier = Supplier();
        supplier.Update("Distribuidora Andina S.R.L.", "Jorge", "2-2400000", "ventas@andina.bo", "Av. 6 de Agosto", Now);

        Assert.Equal("1020304050", supplier.TaxId);
        Assert.Equal("Distribuidora Andina S.R.L.", supplier.Name);
        Assert.Throws<DomainException>(() => supplier.Update("Andina", "", "222", "correo-sin-arroba", "", Now));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-030")]
    [Trait("Requirement", "RF-031")]
    public void El_total_de_la_compra_se_calcula_automaticamente()
    {
        var purchase = Purchase.Create(Guid.NewGuid(), "OC-00001", Supplier(), "operaciones", Now);
        purchase.AddLine(Product(), 100, 0.30m);
        purchase.AddLine(Product(), 50, 1.605m);

        Assert.Equal(110.50m, purchase.Total); // 1,605 se redondea a 1,61 por unidad
        Assert.Equal(PurchaseStatus.Pendiente, purchase.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-030")]
    public void No_se_compra_a_un_proveedor_desactivado_ni_con_cantidades_invalidas()
    {
        var inactive = Supplier();
        inactive.SetActive(false, Now);
        Assert.Throws<DomainException>(() => Purchase.Create(Guid.NewGuid(), "OC-1", inactive, "op", Now));

        var purchase = Purchase.Create(Guid.NewGuid(), "OC-1", Supplier(), "op", Now);
        Assert.Throws<DomainException>(() => purchase.AddLine(Product(), 0, 1m));
        Assert.Throws<DomainException>(() => purchase.AddLine(Product(), 5, 0m));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-032")]
    [Trait("Requirement", "RF-033")]
    [Trait("Requirement", "RF-020")]
    public void Recepcion_completa_crea_el_lote_con_las_100_unidades()
    {
        var (purchase, line) = PurchaseOf(100);

        var received = purchase.Receive([new ReceptionItem(line.Id, "AMX-2511", Today.AddDays(400), 100, 0)],
            "operaciones", Now, Today);

        var lot = Assert.Single(received).Lot;
        Assert.Equal(100, lot.Quantity);
        Assert.Equal("AMX-2511", lot.LotNumber);
        Assert.Equal(line.ProductId, lot.ProductId);
        Assert.Equal(lot.Id, line.LotId);
        Assert.Equal(0, line.Difference);
        Assert.Equal(PurchaseStatus.Recibida, purchase.Status);
        Assert.Equal("operaciones", purchase.ReceivedBy);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-107")]
    [Trait("Requirement", "RF-108")]
    public void Con_10_danadas_solo_ingresan_90_y_se_muestra_la_diferencia()
    {
        var (purchase, line) = PurchaseOf(100);

        var received = purchase.Receive([new ReceptionItem(line.Id, "AMX-2511", Today.AddDays(400), 90, 10)],
            "operaciones", Now, Today);

        Assert.Equal(90, Assert.Single(received).Lot.Quantity);
        Assert.Equal(10, line.Difference);
        Assert.Equal(10, line.DamagedQuantity);
        Assert.Equal(0, line.MissingQuantity);
        Assert.Equal(135m, purchase.ReceivedTotal);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-107")]
    public void Una_linea_sin_recepcion_queda_como_faltante_y_no_crea_lote()
    {
        var purchase = Purchase.Create(Guid.NewGuid(), "OC-00001", Supplier(), "operaciones", Now);
        purchase.AddLine(Product(), 100, 0.30m);
        purchase.AddLine(Product(), 50, 1.60m);
        var first = purchase.Lines[0];

        var received = purchase.Receive([new ReceptionItem(first.Id, "PCM-2511", Today.AddDays(300), 100, 0)],
            "operaciones", Now, Today);

        Assert.Single(received);
        Assert.Equal(50, purchase.Lines[1].MissingQuantity);
        Assert.Null(purchase.Lines[1].LotId);
    }

    [Theory]
    [InlineData("", 400, 10, 0, "lote")]
    [InlineData("L-1", 0, 10, 0, "vencido")]
    [InlineData("L-1", 400, 90, 20, "se pidieron 100")]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-032")]
    public void Rechaza_recepciones_invalidas(string lot, int days, int good, int damaged, string message)
    {
        var (purchase, line) = PurchaseOf(100);

        var error = Assert.Throws<DomainException>(() => purchase.Receive(
            [new ReceptionItem(line.Id, lot, Today.AddDays(days), good, damaged)], "op", Now, Today));

        Assert.Contains(message, error.Message);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-032")]
    public void Una_compra_se_recibe_una_sola_vez()
    {
        var (purchase, line) = PurchaseOf(10);
        var item = new ReceptionItem(line.Id, "L-1", Today.AddDays(100), 10, 0);
        purchase.Receive([item], "op", Now, Today);

        Assert.Throws<DomainException>(() => purchase.Receive([item], "op", Now, Today));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("Requirement", "RF-002")]
    public void Compras_y_proveedores_segun_el_rol()
    {
        Assert.True(RolePermissions.Has(Roles.GerenteOperaciones, Permission.Compras));
        Assert.False(RolePermissions.Has(Roles.GerenteOperaciones, Permission.Proveedores));
        Assert.True(RolePermissions.Has(Roles.Administrador, Permission.Proveedores));
        Assert.False(RolePermissions.Has(Roles.Cajero, Permission.Compras));
    }
}
