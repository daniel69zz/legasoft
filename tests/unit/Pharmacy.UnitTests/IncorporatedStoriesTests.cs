using FluentValidation;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Cash;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Inventory;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Purchasing;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>Usuario del puesto que cambia entre turnos (cajero saliente y entrante).</summary>
internal sealed class SwitchableUser : ICurrentUser
{
    public string UserName { get; set; } = "cajero1";
    public string Role => "Cajero";
    public string DeviceId => "PUESTO-01";
}

/// <summary>Historias incorporadas al Sprint 2 (SCRUM-121 a 131) en Pharmacy.Domain y Pharmacy.Application.</summary>
public class IncorporatedStoriesTests
{
    private readonly FakeStore _store = new();

    [Fact]
    [Trait("JiraIssue", "SCRUM-121")]
    [Trait("Requirement", "RF-025")]
    public async Task El_retiro_da_de_baja_un_lote_vencido_con_su_motivo()
    {
        var product = Product();
        var expired = Lot(product, "IBU-2312", -10, 40);
        _store.Products.Add(product);
        _store.Lots.Add(expired);
        var handler = new AdjustStockHandler(_store, _store, _store, new FixedClock(), new Cashier());

        await handler.HandleAsync(new AdjustStockCommand(product.Id, expired.Id, StockMovementType.Retiro, 40, "Vencido"));

        Assert.Equal(0, expired.Quantity);
        var movement = Assert.Single(_store.Movements);
        Assert.Equal((StockMovementType.Retiro, -40, "Vencido"), (movement.Type, movement.Quantity, movement.Reason));
        await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(
            new AdjustStockCommand(product.Id, expired.Id, StockMovementType.Retiro, 1, "")));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-122")]
    [Trait("JiraIssue", "SCRUM-130")]
    [Trait("Requirement", "RF-021")]
    [Trait("Requirement", "RF-099")]
    public async Task El_detalle_por_lote_usa_el_umbral_de_vencimiento_configurado()
    {
        var product = Product();
        _store.Products.Add(product);
        _store.Lots.AddRange([Lot(product, "PCM-2409", 400, 500), Lot(product, "PCM-2401", 75, 120),
            Lot(product, "PCM-2312", -1, 3), Lot(product, "PCM-VACIO", 30, 0)]);
        var queries = new InventoryQueries(_store, new FixedClock(), _store);

        var with60 = await queries.GetLotDetailsAsync(product.Id);
        _store.ExpiryThresholdDays = 90;
        var with90 = await queries.GetLotDetailsAsync(product.Id);

        Assert.Equal(["PCM-2312", "PCM-2401", "PCM-2409"], with60.Select(l => l.LotNumber));
        Assert.Equal(["Vencido", "Vigente", "Vigente"], with60.Select(l => l.StatusLabel));
        Assert.Equal("Por vencer", with90.Single(l => l.LotNumber == "PCM-2401").StatusLabel);
        Assert.Equal(90, (await queries.GetOverviewAsync()).ExpiryThresholdDays);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-124")]
    [Trait("Requirement", "RF-034")]
    [Trait("Requirement", "RF-035")]
    public void El_precio_vigente_es_el_ultimo_registrado_y_los_anteriores_quedan_en_el_historial()
    {
        var supplier = Guid.NewGuid();
        var product = Guid.NewGuid();
        var history = new[]
        {
            new SupplierPrice(Guid.NewGuid(), supplier, product, 0.30m, "operaciones", Now.AddDays(-5)),
            new SupplierPrice(Guid.NewGuid(), supplier, product, 0.28m, "operaciones", Now),
        };

        Assert.Equal(0.28m, Assert.Single(SupplierPrice.Current(history)).Price);
        Assert.Throws<DomainException>(() => new SupplierPrice(Guid.NewGuid(), supplier, product, 0m, "op", Now));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-125")]
    [Trait("Requirement", "RF-036")]
    public void La_comparacion_ordena_de_menor_a_mayor_y_omite_proveedores_desactivados()
    {
        Guid andina = Guid.NewGuid(), illimani = Guid.NewGuid(), inactivo = Guid.NewGuid(), product = Guid.NewGuid();
        var prices = new[]
        {
            new SupplierPrice(Guid.NewGuid(), andina, product, 1.60m, "op", Now),
            new SupplierPrice(Guid.NewGuid(), illimani, product, 1.70m, "op", Now.AddDays(-3)),
            new SupplierPrice(Guid.NewGuid(), illimani, product, 1.45m, "op", Now),
            new SupplierPrice(Guid.NewGuid(), inactivo, product, 0.90m, "op", Now),
        };

        var compared = SupplierPrice.Compare(prices, product, new HashSet<Guid> { andina, illimani });

        Assert.Equal([(illimani, 1.45m), (andina, 1.60m)], compared.Select(p => (p.SupplierId, p.Price)));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-126")]
    [Trait("Requirement", "RF-061")]
    [Trait("Requirement", "RF-062")]
    public async Task La_entrega_queda_pendiente_hasta_que_el_entrante_confirma_y_abre_su_turno()
    {
        var user = new SwitchableUser();
        var cash = new CashHandlers(_store, _store, _store, new FixedClock(), user);
        await cash.OpenAsync(450m);
        await cash.CloseAsync(new Dictionary<decimal, int> { [200m] = 2, [50m] = 1 }, null);

        await cash.RequestHandoverAsync("cajero2");

        // Mientras cajero2 no confirme, nadie abre caja en el puesto.
        user.UserName = "cajero2";
        var blocked = await Assert.ThrowsAsync<DomainException>(() => cash.OpenAsync(100m));
        Assert.Contains("RF-062", blocked.Message);

        Assert.False(await cash.ConfirmHandoverAsync(400m, agrees: true)); // monto distinto: sigue pendiente
        Assert.Equal(CashSessionStatus.EntregaPendiente, _store.Sessions[0].Status);

        Assert.True(await cash.ConfirmHandoverAsync(450m, agrees: true));
        Assert.Equal(CashSessionStatus.Entregada, _store.Sessions[0].Status);
        var turn = Assert.Single(_store.Sessions, s => s.Status == CashSessionStatus.Abierta);
        Assert.Equal(("cajero2", 450m), (turn.CashierName, turn.OpeningAmount));
        Assert.Equal(
            [EventTypes.CashHandoverRequested, EventTypes.CashHandoverConfirmed, EventTypes.CashHandoverConfirmed,
                EventTypes.CashSessionOpened],
            _store.Outbox.Skip(2).Select(m => m.EventType));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-126")]
    [Trait("Requirement", "RF-061")]
    public async Task Solo_el_cajero_que_cerro_entrega_y_solo_el_entrante_confirma()
    {
        var user = new SwitchableUser();
        var cash = new CashHandlers(_store, _store, _store, new FixedClock(), user);
        await cash.OpenAsync(100m);
        await cash.CloseAsync(new Dictionary<decimal, int> { [100m] = 1 }, null);

        user.UserName = "cajero2";
        await Assert.ThrowsAsync<DomainException>(() => cash.RequestHandoverAsync("cajero3"));

        user.UserName = "cajero1";
        await cash.RequestHandoverAsync("cajero2");
        user.UserName = "cajero3";
        await Assert.ThrowsAsync<DomainException>(() => cash.ConfirmHandoverAsync(100m, agrees: true));
    }
}
