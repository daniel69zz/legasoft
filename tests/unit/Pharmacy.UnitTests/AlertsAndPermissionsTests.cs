using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

public class AlertsAndPermissionsTests
{
    [Theory]
    [InlineData(-1, ExpiryStatus.Vencido)]
    [InlineData(0, ExpiryStatus.Vencido)]
    [InlineData(60, ExpiryStatus.PorVencer)]
    [InlineData(61, ExpiryStatus.Vigente)]
    [Trait("JiraIssue", "SCRUM-17")]
    [Trait("Requirement", "RF-022")]
    public void Clasifica_vencimientos_con_umbral_de_60_dias(int days, ExpiryStatus expected)
    {
        Assert.Equal(expected, InventoryAlerts.StatusOf(Lot(Product(), "A", days, 10), Today));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-16")]
    [Trait("Requirement", "RF-018")]
    public void Alerta_de_stock_minimo_usa_solo_stock_vigente()
    {
        var p = Product(minimum: 10);
        var alerts = InventoryAlerts.LowStock([p], [Lot(p, "A", 30, 8), Lot(p, "V", -2, 500)], Today);

        Assert.Equal(8, Assert.Single(alerts).Stock);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-10")]
    [Trait("Requirement", "RF-002")]
    public void El_cajero_no_gestiona_usuarios_y_el_administrador_si()
    {
        Assert.False(RolePermissions.Has(Roles.Cajero, Permission.Usuarios));
        Assert.True(RolePermissions.Has(Roles.Cajero, Permission.Ventas));
        Assert.All(Enum.GetValues<Permission>(), p => Assert.True(RolePermissions.Has(Roles.Administrador, p)));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-10")]
    [Trait("Requirement", "RF-002")]
    public void Todos_los_roles_tienen_al_menos_un_permiso()
    {
        Assert.All(Roles.All, r => Assert.NotEmpty(RolePermissions.For(r)));
    }
}
