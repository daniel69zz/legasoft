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
}
