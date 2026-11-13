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
}
