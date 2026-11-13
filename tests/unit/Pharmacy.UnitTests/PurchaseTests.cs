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
    [Trait("Requirement", "RF-002")]
    public void Compras_y_proveedores_segun_el_rol()
    {
        Assert.True(RolePermissions.Has(Roles.GerenteOperaciones, Permission.Compras));
        Assert.False(RolePermissions.Has(Roles.GerenteOperaciones, Permission.Proveedores));
        Assert.True(RolePermissions.Has(Roles.Administrador, Permission.Proveedores));
        Assert.False(RolePermissions.Has(Roles.Cajero, Permission.Compras));
    }
}
