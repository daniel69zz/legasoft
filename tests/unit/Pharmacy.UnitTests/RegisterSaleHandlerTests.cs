using FluentValidation;
using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Cash;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sales;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;
using static Pharmacy.UnitTests.TestData;

namespace Pharmacy.UnitTests;

/// <summary>Repositorios en memoria que registran si se confirmó la transacción.</summary>
internal sealed class FakeStore : ICatalogRepository, ISalesRepository, ICashRepository, IOutbox, IUnitOfWork
{
    public List<Product> Products { get; } = [];
    public List<Lot> Lots { get; } = [];
    public List<StockMovement> Movements { get; } = [];
    public List<Sale> Sales { get; } = [];
    public List<CashSession> Sessions { get; } = [];
    public List<OutboxMessage> Outbox { get; } = [];
    public int Commits { get; private set; }

    public Task<List<Product>> GetProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        Task.FromResult(Products.Where(p => ids.Contains(p.Id)).ToList());
    public Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default) => Task.FromResult(Products.ToList());
    public Task<List<Lot>> GetLotsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        Task.FromResult(Lots.Where(l => ids.Contains(l.ProductId)).ToList());
    public Task<List<Lot>> GetAllLotsAsync(CancellationToken ct = default) => Task.FromResult(Lots.ToList());
    public void AddMovement(StockMovement movement) => Movements.Add(movement);
    public void Add(Sale sale) => Sales.Add(sale);
    public Task<Sale?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Sales.SingleOrDefault(s => s.Id == id));
    public Task<int> CountByDeviceAsync(string deviceId, CancellationToken ct = default) => Task.FromResult(Sales.Count);
    public void Add(CashSession session) => Sessions.Add(session);
    public Task<CashSession?> GetOpenAsync(string deviceId, CancellationToken ct = default) =>
        Task.FromResult(Sessions.SingleOrDefault(s => s.DeviceId == deviceId && s.Status == CashSessionStatus.Abierta));
    public void Add(OutboxMessage message) => Outbox.Add(message);
    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        Commits++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock : IClock
{
    public DateTimeOffset Now => TestData.Now;
}

internal sealed class Cashier : ICurrentUser
{
    public string UserName => "cajero1";
    public string Role => "Cajero";
    public string DeviceId => "PUESTO-01";
}

public class RegisterSaleHandlerTests
{
    private readonly FakeStore _store = new();
    private readonly RegisterSaleHandler _handler;
    private readonly CashHandlers _cash;
    private readonly Product _product = Product(price: 2.50m);

    public RegisterSaleHandlerTests()
    {
        _handler = new RegisterSaleHandler(_store, _store, _store, _store, _store, new FixedClock(), new Cashier());
        _cash = new CashHandlers(_store, _store, _store, new FixedClock(), new Cashier());
        _store.Products.Add(_product);
        _store.Lots.Add(Lot(_product, "TARDE", 300, 100));
        _store.Lots.Add(Lot(_product, "PRONTO", 20, 4));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-21")]
    [Trait("Requirement", "RF-037")]
    public async Task Sin_caja_abierta_no_se_vende()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(new RegisterSaleCommand([new(_product.Id, 1)], 10m)));
        Assert.Equal(0, _store.Commits);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-21")]
    [Trait("Requirement", "RF-046")]
    public async Task Venta_aplica_FEFO_descuenta_stock_registra_caja_y_outbox_en_una_transaccion()
    {
        await _cash.OpenAsync(100m);
        var commitsBefore = _store.Commits;

        var receipt = await _handler.HandleAsync(new RegisterSaleCommand([new(_product.Id, 6)], 20m));

        Assert.Equal(15m, receipt.Total);
        Assert.Equal(5m, receipt.Change);
        Assert.Equal("PUESTO-01-000001", receipt.ReceiptNumber);
        Assert.Equal(0, _store.Lots.Single(l => l.LotNumber == "PRONTO").Quantity);
        Assert.Equal(98, _store.Lots.Single(l => l.LotNumber == "TARDE").Quantity);
        Assert.Equal(-6, _store.Movements.Sum(m => m.Quantity));
        Assert.Equal(115m, _store.Sessions.Single().ExpectedAmount);
        var message = _store.Outbox.Last();
        Assert.Equal((SyncModules.Sales, EventTypes.SaleCreated), (message.Module, message.EventType));
        Assert.Equal(commitsBefore + 1, _store.Commits);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-21")]
    [Trait("Requirement", "RF-046")]
    public async Task Si_una_regla_falla_no_se_confirma_nada()
    {
        await _cash.OpenAsync(100m);
        var commitsBefore = _store.Commits;

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(new RegisterSaleCommand([new(_product.Id, 500)], 2000m)));

        Assert.Equal(commitsBefore, _store.Commits);
        Assert.Empty(_store.Sales);
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-21")]
    [Trait("Requirement", "RF-037")]
    public async Task Valida_el_comando_antes_de_ejecutar()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.HandleAsync(new RegisterSaleCommand([], 0m)));
    }

    [Fact]
    [Trait("JiraIssue", "SCRUM-23")]
    [Trait("Requirement", "RF-048")]
    public async Task Anular_devuelve_el_stock_a_su_lote()
    {
        await _cash.OpenAsync(100m);
        var receipt = await _handler.HandleAsync(new RegisterSaleCommand([new(_product.Id, 2)], 10m));
        var handler = new VoidSaleHandler(_store, _store, _store, _store, _store, new FixedClock(), new Cashier());

        await handler.HandleAsync(new VoidSaleCommand(receipt.SaleId, "Cliente desistió"));

        Assert.Equal(4, _store.Lots.Single(l => l.LotNumber == "PRONTO").Quantity);
        Assert.Equal(100m, _store.Sessions.Single().ExpectedAmount);
        Assert.Equal(EventTypes.SaleVoided, _store.Outbox.Last().EventType);
    }
}
