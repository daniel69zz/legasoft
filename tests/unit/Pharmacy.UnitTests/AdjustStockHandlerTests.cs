using System.Text.Json;
using FluentValidation;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Inventory;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>HU-06.3: entradas, salidas y ajustes manuales con motivo (CP-RF-014 a CP-RF-016).</summary>
public class AdjustStockHandlerTests
{
    private readonly FakeStore _store = new();
    private readonly AdjustStockHandler _handler;
    private readonly Lot _lot;

    public AdjustStockHandlerTests()
    {
        _handler = new AdjustStockHandler(_store, _store, _store, new FixedClock(), new Cashier());
        var product = Product();
        _store.Products.Add(product);
        _lot = Lot(product, "AMX-2405", 200, 50);
        _store.Lots.Add(_lot);
    }

    private AdjustStockCommand Command(StockMovementType type, int quantity, string reason = "Producto dañado") =>
        new(_lot.ProductId, _lot.Id, type, quantity, reason);

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    public async Task Ajuste_de_menos_3_deja_47_y_guarda_usuario_fecha_y_motivo()
    {
        await _handler.HandleAsync(Command(StockMovementType.Ajuste, -3));

        Assert.Equal(47, _lot.Quantity);
        var movement = Assert.Single(_store.Movements);
        Assert.Equal((StockMovementType.Ajuste, -3), (movement.Type, movement.Quantity));
        Assert.Equal(("cajero1", "Producto dañado", Now), (movement.UserName, movement.Reason, movement.OccurredAt));
        Assert.Equal(1, _store.Commits);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    public async Task Un_ajuste_sin_motivo_se_rechaza_y_el_stock_no_cambia()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.HandleAsync(Command(StockMovementType.Ajuste, -3, "  ")));

        Assert.Equal(50, _lot.Quantity);
        Assert.Empty(_store.Movements);
        Assert.Equal(0, _store.Commits);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-014")]
    [Trait("Requirement", "RF-015")]
    public async Task Entrada_suma_y_salida_resta_unidades_del_lote()
    {
        await _handler.HandleAsync(Command(StockMovementType.Entrada, 5, "Devolución de cliente"));
        await _handler.HandleAsync(Command(StockMovementType.Salida, 8, "Muestra para control de calidad"));

        Assert.Equal(47, _lot.Quantity);
        Assert.Equal([5, -8], _store.Movements.Select(m => m.Quantity));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-015")]
    public async Task No_se_retiran_mas_unidades_de_las_que_tiene_el_lote()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(Command(StockMovementType.Salida, 51, "Baja")));
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.HandleAsync(Command(StockMovementType.Venta, -1, "Venta manual")));

        Assert.Equal(50, _lot.Quantity);
        Assert.Equal(0, _store.Commits);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    public async Task El_movimiento_viaja_al_servidor_por_el_outbox_en_la_misma_transaccion()
    {
        await _handler.HandleAsync(Command(StockMovementType.Ajuste, -3));

        var message = Assert.Single(_store.Outbox);
        Assert.Equal((SyncModules.Inventory, EventTypes.StockMovementCreated), (message.Module, message.EventType));
        var payload = JsonSerializer.Deserialize<StockMovementPayload>(message.Payload, OutboxMessage.Json)!;
        Assert.Equal((_lot.Id, -3, "Ajuste", "Producto dañado"), (payload.LotId, payload.Quantity, payload.Type, payload.Reason));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    public async Task El_historial_muestra_tipo_lote_cantidad_y_motivo()
    {
        await _handler.HandleAsync(Command(StockMovementType.Ajuste, -3));
        var queries = new InventoryQueries(_store, new FixedClock(), _store);

        var row = Assert.Single(await queries.GetMovementsAsync(_lot.ProductId));

        Assert.Equal(("Ajuste", "AMX-2405", -3, "Producto dañado", "cajero1"),
            (row.Type, row.LotNumber, row.Quantity, row.Reason, row.UserName));
    }
}
