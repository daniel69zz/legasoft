using Pharmacy.Domain.Common;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>HU-05: reglas del catálogo de medicamentos (RF-009 a RF-012).</summary>
public class ProductTests
{
    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-009")]
    public void Un_medicamento_nuevo_queda_activo_y_exige_codigo_nombre_y_precio()
    {
        Assert.True(Product().IsActive);
        Assert.Throws<DomainException>(() => new Pharmacy.Domain.Catalog.Product(Guid.NewGuid(), " ", "Ibuprofeno",
            "Ibuprofeno", "400 mg", "Tableta", "Lab", "Analgésicos", false, 1m, 0));
        Assert.Throws<DomainException>(() => new Pharmacy.Domain.Catalog.Product(Guid.NewGuid(), "P-9", " ",
            "Ibuprofeno", "400 mg", "Tableta", "Lab", "Analgésicos", false, 1m, 0));
        Assert.Throws<DomainException>(() => Product(price: 0m));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-010")]
    public void Modificar_actualiza_los_datos_sin_cambiar_el_codigo()
    {
        var p = Product();
        p.UpdateDetails("Amoxil", "Amoxicilina", "875 mg", "Tableta", "Bagó", "Antibióticos", true, 3.20m, 15,
            "Estante A2", Now);

        Assert.Equal("P-0001", p.Code);
        Assert.Equal("Amoxil", p.Name);
        Assert.Equal("875 mg", p.Concentration);
        Assert.True(p.RequiresPrescription);
        Assert.Equal(3.20m, p.SalePrice);
        Assert.Equal(Now, p.UpdatedAt);
        Assert.Throws<DomainException>(() =>
            p.UpdateDetails("Amoxil", "", "", "", "", "", false, -1m, 0, "", Now));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-011")]
    public void Desactivar_y_reactivar_conserva_los_datos()
    {
        var p = Product();
        p.SetActive(false, Now);
        Assert.False(p.IsActive);
        p.SetActive(true, Now);
        Assert.True(p.IsActive);
        Assert.Equal("Amoxicilina", p.Name);
    }

    [Theory]
    [InlineData("p-0001")]
    [InlineData("amoxi")]
    [InlineData("ANTIBIÓ")]
    [InlineData("")]
    [Trait("JiraIssue", "SCRUM-15")]
    [Trait("Requirement", "RF-012")]
    public void Busca_por_codigo_nombre_principio_activo_o_categoria(string term)
    {
        Assert.True(Product().Matches(term));
        Assert.False(Product().Matches("paracetamol"));
    }
}
