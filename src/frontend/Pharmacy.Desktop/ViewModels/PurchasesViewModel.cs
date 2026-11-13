using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

public sealed record ProductOption(Guid Id, string Code, string Name)
{
    public override string ToString() => $"{Code} · {Name}";
}

/// <summary>Línea de la compra en preparación (RF-030).</summary>
public sealed partial class PurchaseDraftLine(ProductOption product, Action changed) : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Subtotal))] private decimal? _quantity = 1;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Subtotal))] private decimal? _unitCost;

    public ProductOption Product { get; } = product;
    public decimal Subtotal => Math.Round((Quantity ?? 0) * (UnitCost ?? 0), 2);

    partial void OnQuantityChanged(decimal? value) => changed();
    partial void OnUnitCostChanged(decimal? value) => changed();
}

/// <summary>Línea a recibir: lote, vencimiento, unidades en buen estado y dañadas (RF-032, RF-107, RF-108).</summary>
public sealed partial class ReceptionRow(PurchaseLineDto line) : ObservableObject
{
    [ObservableProperty] private string _lotNumber = "";
    [ObservableProperty] private DateTime? _expirationDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Difference), nameof(Missing), nameof(HasDifference))]
    private decimal? _received = line.Quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Missing), nameof(HasDifference))]
    private decimal? _damaged = 0;

    public PurchaseLineDto Line { get; } = line;

    /// <summary>RF-107: lo pedido menos lo que ingresa en buen estado.</summary>
    public int Difference => Line.Quantity - (int)(Received ?? 0);

    public int Missing => Line.Quantity - (int)(Received ?? 0) - (int)(Damaged ?? 0);
    public bool HasDifference => Difference != 0;

    public ReceptionLineRequest ToRequest() => new(Line.Id, LotNumber,
        ExpirationDate is { } d ? DateOnly.FromDateTime(d) : null, (int)(Received ?? 0), (int)(Damaged ?? 0));
}

/// <summary>
/// HU-09: registro de compras y su recepción (RF-030 a RF-033, RF-107, RF-108). Se trabaja en
/// línea; los lotes recibidos llegan al stock del puesto con la siguiente sincronización.
/// </summary>
public sealed partial class PurchasesViewModel(ApiClient api, Operations operations, SyncWorker sync)
    : ViewModelBase, IActivatable
{
    [ObservableProperty] private SupplierDto? _supplier;
    [ObservableProperty] private ProductOption? _productToAdd;
    [ObservableProperty] private PurchaseDto? _selected;
    [ObservableProperty] private string _receptionSummary = "";
    [ObservableProperty] private decimal? _newPrice;

    /// <summary>HU-10.1 (RF-034): precios vigentes del proveedor elegido.</summary>
    public ObservableCollection<SupplierPriceDto> SupplierPrices { get; } = [];

    /// <summary>HU-10.1 (RF-035): historial del producto elegido con el proveedor elegido.</summary>
    public ObservableCollection<SupplierPriceDto> PriceHistory { get; } = [];

    public bool HasProduct => ProductToAdd is not null;

    partial void OnSupplierChanged(SupplierDto? value) => _ = RefreshPricesAsync();

    partial void OnProductToAddChanged(ProductOption? value)
    {
        OnPropertyChanged(nameof(HasProduct));
        _ = RefreshPricesAsync();
    }

    // Sin RunAsync: refrescar precios no debe borrar el mensaje de la última operación.
    private async Task RefreshPricesAsync()
    {
        try
        {
            var supplier = Supplier;
            var product = ProductToAdd;
            var prices = supplier is null ? [] : await api.GetSupplierPricesAsync(supplier.Id);
            var history = supplier is null || product is null ? [] : await api.GetPriceHistoryAsync(supplier.Id, product.Id);
            Replace(SupplierPrices, prices);
            Replace(PriceHistory, history);
        }
        catch (Exception e) when (e is ApiException or HttpRequestException or TaskCanceledException)
        {
            Error = e.Message;
        }
    }

    [RelayCommand]
    private Task RegisterPriceAsync()
    {
        var supplier = Supplier;
        var product = ProductToAdd;
        return RunAsync(async () =>
        {
            if (supplier is null) throw new Domain.Common.DomainException("Elija el proveedor.");
            if (product is null) throw new Domain.Common.DomainException("Elija el producto.");
            var saved = await api.RegisterSupplierPriceAsync(supplier.Id,
                new RegisterSupplierPriceRequest(product.Id, NewPrice ?? 0));
            NewPrice = null;
            await RefreshPricesAsync();
            Info = $"Precio vigente de {saved.SupplierName} para {saved.ProductName}: Bs {saved.Price:N2}.";
        });
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<ProductOption> ProductOptions { get; } = [];
    public ObservableCollection<PurchaseDraftLine> Draft { get; } = [];
    public ObservableCollection<PurchaseDto> Purchases { get; } = [];
    public ObservableCollection<ReceptionRow> Reception { get; } = [];

    /// <summary>RF-031: total calculado de la compra en preparación.</summary>
    public decimal DraftTotal => Draft.Sum(l => l.Subtotal);

    public bool CanReceive => Selected?.Status == "Pendiente";
    public bool IsReceived => Selected?.Status == "Recibida";

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var suppliers = await api.GetSuppliersAsync();
        Suppliers.Clear();
        foreach (var s in suppliers.Where(s => s.IsActive)) Suppliers.Add(s);

        var products = await operations.QueryAsync(db => db.Products.Where(p => p.IsActive).ToListAsync());
        ProductOptions.Clear();
        foreach (var p in products.OrderBy(p => p.Name)) ProductOptions.Add(new ProductOption(p.Id, p.Code, p.DisplayName));

        await LoadPurchasesAsync();
    });

    private async Task LoadPurchasesAsync()
    {
        var purchases = await api.GetPurchasesAsync();
        var selectedId = Selected?.Id;
        Purchases.Clear();
        foreach (var p in purchases) Purchases.Add(p);
        Selected = Purchases.FirstOrDefault(p => p.Id == selectedId);
    }

    partial void OnSelectedChanged(PurchaseDto? value)
    {
        OnPropertyChanged(nameof(CanReceive));
        OnPropertyChanged(nameof(IsReceived));
        Reception.Clear();
        ReceptionSummary = "";
        if (value is null) return;
        if (value.Status == "Pendiente")
            foreach (var line in value.Lines) Reception.Add(new ReceptionRow(line));
        else
            ReceptionSummary = Summary(value);
    }

    [RelayCommand]
    private void AddProduct()
    {
        if (ProductToAdd is not { } product) return;
        if (Draft.Any(l => l.Product.Id == product.Id))
        {
            Error = $"{product.Name} ya está en la compra.";
            return;
        }
        // HU-10.1: el costo se propone con el precio vigente del proveedor elegido.
        var line = new PurchaseDraftLine(product, () => OnPropertyChanged(nameof(DraftTotal)))
        {
            UnitCost = SupplierPrices.FirstOrDefault(p => p.ProductId == product.Id)?.Price,
        };
        Draft.Add(line);
        ProductToAdd = null;
        OnPropertyChanged(nameof(DraftTotal));
    }

    [RelayCommand]
    private void RemoveLine(PurchaseDraftLine line)
    {
        Draft.Remove(line);
        OnPropertyChanged(nameof(DraftTotal));
    }

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        var request = new CreatePurchaseRequest(Supplier?.Id ?? Guid.Empty,
            Draft.Select(l => new PurchaseItemRequest(l.Product.Id, (int)(l.Quantity ?? 0), l.UnitCost ?? 0)).ToList());
        var created = await api.CreatePurchaseAsync(request);
        Draft.Clear();
        OnPropertyChanged(nameof(DraftTotal));
        await LoadPurchasesAsync();
        Selected = Purchases.FirstOrDefault(p => p.Id == created.Id);
        Info = $"Compra {created.Number} registrada por Bs {created.Total:N2}. Pendiente de recepción.";
    });

    [RelayCommand]
    private Task ReceiveAsync()
    {
        var purchase = Selected;
        if (purchase is null) return Task.CompletedTask;
        return RunAsync(async () =>
        {
            var received = await api.ReceivePurchaseAsync(purchase.Id,
                new ReceivePurchaseRequest(Reception.Select(r => r.ToRequest()).ToList()));
            await LoadPurchasesAsync();
            Selected = Purchases.FirstOrDefault(p => p.Id == received.Id);
            sync.Wake(); // descarga los lotes nuevos al puesto
            Info = received.Lines.Any(l => l.Difference != 0)
                ? $"Compra {received.Number} recibida con diferencias. Solo ingresó al stock lo recibido en buen estado."
                : $"Compra {received.Number} recibida completa. El stock ya se actualizó.";
        });
    }

    private static string Summary(PurchaseDto p) =>
        $"Recibida por {p.ReceivedBy} el {p.ReceivedAt?.ToLocalTime():dd/MM/yyyy HH:mm}. " +
        $"Ingresó Bs {p.ReceivedTotal:N2} de Bs {p.Total:N2}.";
}
