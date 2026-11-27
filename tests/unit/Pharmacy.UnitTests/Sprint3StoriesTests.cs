using System.Globalization;
using System.Text;
using System.Text.Json;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Cash;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Sales;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

internal sealed class Pharmacist : ICurrentUser
{
    public string UserName => "farmaceutico";
    public string Role => "Farmaceutico";
    public string DeviceId => "PUESTO-01";
}

/// <summary>
/// Historias del Sprint 3 en Pharmacy.Domain y Pharmacy.Application: cobro con faltante (SCRUM-22), arqueo
/// (SCRUM-25), receta (SCRUM-29), descuentos (SCRUM-30), stock entre puestos (SCRUM-35), agrupación del
/// comparativo (SCRUM-32) y documentos en PDF (SCRUM-24, SCRUM-31).
/// </summary>
public class Sprint3StoriesTests
{
    private readonly FakeStore _store = new();
    private readonly Pharmacy.Domain.Catalog.Product _product = Product(price: 12.50m);
    private readonly Pharmacy.Domain.Catalog.Product _rx = Product(price: 20m, prescription: true);

    public Sprint3StoriesTests()
    {
        _store.Products.AddRange([_product, _rx]);
        _store.Lots.Add(Lot(_product, "LIB-1", 200, 50));
        _store.Lots.Add(Lot(_rx, "RX-1", 200, 50));
    }

    private RegisterSaleHandler Handler(ICurrentUser user) =>
        new(_store, _store, _store, _store, _store, new FixedClock(), user, _store);

    private async Task OpenCashAsync(ICurrentUser user) =>
        await new CashHandlers(_store, _store, _store, new FixedClock(), user).OpenAsync(100m);

    [Fact]
    [Trait("JiraIssue", "SCRUM-22")]
    [Trait("Requirement", "RF-044")]
    [Trait("Requirement", "RF-045")]
    public void Con_efectivo_insuficiente_no_se_cobra_y_se_indica_cuanto_falta()
    {
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-BO");
        try
        {
            var sale = Sale.Start(Guid.NewGuid(), "R-1", "PUESTO-01", "cajero1", Guid.NewGuid(), Now);
            sale.AddLine(_product, FefoAllocator.Allocate(_store.Lots.Where(l => l.ProductId == _product.Id), 3, Today));

            var error = Assert.Throws<DomainException>(() => sale.Complete(30m));
            Assert.Contains("faltan Bs 7,50", error.Message);
            Assert.Equal(7.50m, sale.MissingFor(30m));

            sale.Complete(50m);
            Assert.Equal((37.50m, 12.50m), (sale.Total, sale.Change));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-29")]
    [Trait("Requirement", "RF-088")]
    [Trait("Requirement", "RF-089")]
    [Trait("Requirement", "RF-091")]
    public async Task La_receta_se_registra_completa_y_queda_asociada_a_la_venta()
    {
        var cashier = new Cashier();
        await OpenCashAsync(cashier);

        await Assert.ThrowsAsync<DomainException>(() => Handler(cashier).HandleAsync(
            new RegisterSaleCommand([new(_rx.Id, 1)], 20m, "RX-55")));   // sin médico ni paciente
        await Assert.ThrowsAsync<DomainException>(() => Handler(cashier).HandleAsync(new RegisterSaleCommand(
            [new(_rx.Id, 1)], 20m, "RX-55", PrescriptionDoctor: "Dr. Mamani", PrescriptionPatient: "Ana Choque",
            PrescriptionIssuedOn: Today.AddDays(-45))));                  // vencida

        var receipt = await Handler(cashier).HandleAsync(new RegisterSaleCommand([new(_rx.Id, 1)], 20m, "RX-55",
            PrescriptionDoctor: "Dr. Mamani", PrescriptionPatient: "Ana Choque", PrescriptionIssuedOn: Today));

        var sale = _store.Sales.Single();
        Assert.Equal(("RX-55", "Dr. Mamani", "Ana Choque", Today),
            (sale.PrescriptionNumber, sale.PrescriptionDoctor, sale.PrescriptionPatient, sale.PrescriptionIssuedOn!.Value));
        var payload = JsonSerializer.Deserialize<SaleCreatedPayload>(_store.Outbox.Last().Payload, OutboxMessage.Json)!;
        Assert.Equal("Dr. Mamani", payload.PrescriptionDoctor);
        Assert.Equal("RX-55", receipt.PrescriptionNumber);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-29")]
    [Trait("Requirement", "RF-090")]
    public async Task Un_medicamento_de_venta_libre_no_pide_receta()
    {
        var cashier = new Cashier();
        await OpenCashAsync(cashier);

        var receipt = await Handler(cashier).HandleAsync(new RegisterSaleCommand([new(_product.Id, 1)], 20m));

        Assert.Equal("", receipt.PrescriptionNumber);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-30")]
    [Trait("Requirement", "RF-096")]
    public async Task Solo_quien_tiene_permiso_descuenta_y_hasta_el_maximo_configurado()
    {
        await OpenCashAsync(new Pharmacist());

        // El cajero no tiene permiso: se rechaza y no cambia nada.
        await Assert.ThrowsAsync<DomainException>(() => Handler(new Cashier()).HandleAsync(
            new RegisterSaleCommand([new(_product.Id, 8)], 100m, Discount: 10m)));
        Assert.Empty(_store.Sales);

        // Con permiso, el 20 % de Bs 100 es el máximo: Bs 25 se rechaza y Bs 10 se aplica.
        await Assert.ThrowsAsync<DomainException>(() => Handler(new Pharmacist()).HandleAsync(
            new RegisterSaleCommand([new(_product.Id, 8)], 100m, Discount: 25m)));
        var receipt = await Handler(new Pharmacist()).HandleAsync(
            new RegisterSaleCommand([new(_product.Id, 8)], 100m, Discount: 10m));

        Assert.Equal((100m, 10m, 90m), (receipt.Subtotal, receipt.Discount, receipt.Total));
        Assert.Equal("farmaceutico", _store.Sales.Single().DiscountAuthorizedBy);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-25")]
    [Trait("Requirement", "RF-056")]
    public async Task El_arqueo_registra_la_diferencia_sin_cerrar_la_caja()
    {
        var cash = new CashHandlers(_store, _store, _store, new FixedClock(), new Cashier());
        await cash.OpenAsync(200m);

        var arqueo = await cash.RegisterArqueoAsync(new Dictionary<decimal, int> { [100m] = 1, [50m] = 2, [10m] = 1 },
            "Arqueo de media mañana");

        var session = _store.Sessions.Single();
        Assert.Equal(CashSessionStatus.Abierta, session.Status);
        Assert.Equal((200m, 210m, 10m), (arqueo.ExpectedAmount, arqueo.CountedAmount, arqueo.Difference));
        Assert.Single(session.Arqueos);
        Assert.Equal(200m, session.ExpectedAmount);   // el arqueo no cambia el saldo
        Assert.Equal(EventTypes.CashArqueoRegistered, _store.Outbox.Last().EventType);
        await Assert.ThrowsAsync<DomainException>(() => cash.RegisterArqueoAsync(new Dictionary<decimal, int>(), null));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-35")]
    [Trait("Requirement", "RF-081")]
    public void Los_movimientos_pendientes_del_outbox_se_restan_por_lote()
    {
        var lotA = Guid.NewGuid();
        var lotB = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var sale = OutboxMessage.Create("PUESTO-01", SyncModules.Sales, EventTypes.SaleCreated, saleId,
            new SaleCreatedPayload(saleId, "R", "PUESTO-01", "c", Guid.NewGuid(), Now, 10, 0, 10, 10, 0, "", "", "",
                [new(Guid.NewGuid(), "P", lotA, "A", 4, 2.5m)]), Now);
        var adjustment = OutboxMessage.Create("PUESTO-01", SyncModules.Inventory, EventTypes.StockMovementCreated,
            Guid.NewGuid(), new StockMovementPayload(Guid.NewGuid(), Guid.NewGuid(), lotB, "B", "Ajuste", -3, "x", "a", Now), Now);
        var voided = OutboxMessage.Create("PUESTO-01", SyncModules.Sales, EventTypes.SaleVoided, Guid.NewGuid(),
            new SaleVoidedPayload(Guid.NewGuid(), "error", "c", Now), Now);

        var deltas = PendingLotDeltas.From([sale, adjustment, voided], _ => [(lotA, 1)]);

        Assert.Equal(-3, deltas[lotA]);   // vendió 4 y una anulación devolvió 1
        Assert.Equal(-3, deltas[lotB]);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-32")]
    [Trait("Requirement", "RF-074")]
    public void El_comparativo_agrupa_por_semana_de_lunes_y_por_mes()
    {
        var thursday = new DateOnly(2026, 11, 26);

        Assert.Equal(new DateOnly(2026, 11, 23), SalesGrouping.PeriodStart(thursday, SalesGrouping.Week));
        Assert.Equal(new DateOnly(2026, 11, 1), SalesGrouping.PeriodStart(thursday, SalesGrouping.Month));
        Assert.Equal(thursday, SalesGrouping.PeriodStart(thursday, SalesGrouping.Day));
        Assert.Equal(new DateOnly(2026, 11, 23), SalesGrouping.PeriodStart(new DateOnly(2026, 11, 29), SalesGrouping.Week));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-24")]
    [Trait("JiraIssue", "SCRUM-33")]
    [Trait("Requirement", "RF-050")]
    [Trait("Requirement", "RF-097")]
    public void El_recibo_lleva_los_datos_de_la_empresa_y_se_descarga_en_pdf()
    {
        var company = new CompanyProfileDto("Farmacia Pacheco", "1023456789", "Av. 20 de Octubre", "2-2411111",
            "Sopocachi", "Av. 6 de Agosto 2100");
        var receipt = new SaleReceipt(Guid.NewGuid(), "PUESTO-01-000007", Now, "cajero1",
            [new(Guid.NewGuid(), "Paracetamol 500 mg", Guid.NewGuid(), "PCM-2401", 2, 0.50m)], 1m, 0, 1m, 2m, 1m,
            "María Quispe", "4455667");

        var text = Receipts.Text(company, receipt);
        var pdf = Receipts.Pdf(company, receipt);

        Assert.Contains("FARMACIA PACHECO", text);
        Assert.Contains("NIT 1023456789", text);
        Assert.Contains("Sucursal Sopocachi", text);
        Assert.Contains("Cliente: María Quispe", text);
        Assert.StartsWith("%PDF-1.4", Encoding.Latin1.GetString(pdf, 0, 8));
        Assert.Contains("PUESTO-01-000007", Encoding.Latin1.GetString(pdf));
        Assert.Contains("María Quispe", Encoding.Latin1.GetString(pdf));   // acentos en WinAnsi
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-31")]
    [Trait("Requirement", "RF-068")]
    public void El_reporte_en_texto_alinea_columnas_y_muestra_el_total()
    {
        var table = new ReportTableDto("Reporte de ventas", "Del 01/11/2026 al 15/11/2026", ["Recibo", "Total"],
            [["PUESTO-01-000001", "12,50"], ["PUESTO-01-000002", "7,00"]], ["2 completadas", "19,50"]);

        var lines = ReportPdf.Lines(table).ToList();

        Assert.Equal("Recibo            Total", lines[0]);
        Assert.Equal("PUESTO-01-000002   7,00", lines[3]);
        Assert.Equal("2 completadas     19,50", lines[^1]);
        Assert.StartsWith("%PDF", Encoding.Latin1.GetString(ReportPdf.Build(table, CompanyProfileDto.Default), 0, 4));
    }
}
