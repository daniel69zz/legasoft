using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Common;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>RF-052 a RF-062 y RF-106.</summary>
public class CashSessionTests
{
    private static CashSession OpenWithSales()
    {
        var session = CashSession.Open(Guid.NewGuid(), "PUESTO-01", "cajero1", 200m, Now);
        session.RegisterSale(Guid.NewGuid(), 57.50m, "cajero1", Now);
        session.RegisterExpense(20m, "Agua para la tienda", "Insumos", "cajero1", Now);
        return session;
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-65")]
    [Trait("Requirement", "RF-053")]
    public void Esperado_es_inicial_mas_ventas_menos_egresos()
    {
        Assert.Equal(237.50m, OpenWithSales().ExpectedAmount);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-66")]
    [Trait("Requirement", "RF-059")]
    public void Cierre_sin_diferencia_no_exige_justificacion()
    {
        var session = OpenWithSales();
        session.Close(new CashCount(new Dictionary<decimal, int> { [200m] = 1, [20m] = 1, [10m] = 1, [5m] = 1, [2m] = 1, [0.50m] = 1 }), null, Now);

        Assert.Equal(CashSessionStatus.Cerrada, session.Status);
        Assert.Equal(0m, session.Difference);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-66")]
    [Trait("Requirement", "RF-106")]
    public void Cierre_con_diferencia_exige_justificacion()
    {
        var session = OpenWithSales();
        var count = new CashCount(new Dictionary<decimal, int> { [200m] = 1, [20m] = 1 });

        var ex = Assert.Throws<DomainException>(() => session.Close(count, "", Now));
        Assert.Contains("-17.50", ex.Message.Replace(',', '.'));

        session.Close(count, "Faltante por cambio mal entregado", Now);
        Assert.Equal(-17.50m, session.Difference);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-65")]
    [Trait("Requirement", "RF-055")]
    public void Egreso_no_puede_superar_el_efectivo_disponible()
    {
        var session = CashSession.Open(Guid.NewGuid(), "PUESTO-01", "cajero1", 50m, Now);
        Assert.Throws<DomainException>(() => session.RegisterExpense(80m, "Compra", "Insumos", "cajero1", Now));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-65")]
    [Trait("Requirement", "RF-055")]
    public void Egreso_exige_motivo_y_categoria()
    {
        var session = CashSession.Open(Guid.NewGuid(), "PUESTO-01", "cajero1", 50m, Now);
        Assert.Throws<DomainException>(() => session.RegisterExpense(10m, "", "Insumos", "cajero1", Now));
        Assert.Throws<DomainException>(() => session.RegisterExpense(10m, "Motivo", " ", "cajero1", Now));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-66")]
    [Trait("Requirement", "RF-062")]
    public void La_entrega_queda_pendiente_hasta_la_conformidad_del_cajero_entrante()
    {
        var session = CashSession.Open(Guid.NewGuid(), "PUESTO-01", "cajero1", 100m, Now);
        session.Close(new CashCount(new Dictionary<decimal, int> { [100m] = 1 }), null, Now);
        session.RequestHandover("cajero2");

        session.ConfirmHandover("cajero2", 90m, agrees: true, Now);
        Assert.Equal(CashSessionStatus.EntregaPendiente, session.Status);

        session.ConfirmHandover("cajero2", 100m, agrees: true, Now);
        Assert.Equal(CashSessionStatus.Entregada, session.Status);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-66")]
    [Trait("Requirement", "RF-054")]
    public void El_conteo_rechaza_denominaciones_inexistentes()
    {
        Assert.Throws<DomainException>(() => new CashCount(new Dictionary<decimal, int> { [3m] = 1 }));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-65")]
    [Trait("Requirement", "RF-053")]
    public void No_se_registran_ventas_con_la_caja_cerrada()
    {
        var session = CashSession.Open(Guid.NewGuid(), "PUESTO-01", "cajero1", 0m, Now);
        session.Close(new CashCount(new Dictionary<decimal, int>()), null, Now);
        Assert.Throws<DomainException>(() => session.RegisterSale(Guid.NewGuid(), 10m, "cajero1", Now));
    }
}
