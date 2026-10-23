using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Inventory;
using Pharmacy.Application.Sales;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Sales;

namespace Pharmacy.Desktop.ViewModels;

public sealed partial class CartItem(StockRow product, Action changed) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _quantity = 1;

    public StockRow Product { get; } = product;
    public decimal Subtotal => Product.SalePrice * Quantity;

    partial void OnQuantityChanged(int value) => changed();
}

public sealed record RecentSale(Guid Id, string ReceiptNumber, DateTimeOffset CreatedAt, decimal Total, string Status)
{
    public bool CanVoid => Status == nameof(SaleStatus.Completada);
}

/// <summary>Punto de venta (HU de ventas, RF-037 a RF-049): carrito, FEFO, receta y cambio.</summary>
public sealed partial class PosViewModel(Operations operations) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ChangePreview))] private decimal? _cashReceived;
    [ObservableProperty] private string _prescriptionNumber = "";
    [ObservableProperty] private string _customerName = "";
    [ObservableProperty] private string _customerTaxId = "";
    [ObservableProperty] private string _receipt = "";
    [ObservableProperty] private string _voidReason = "";

    public ObservableCollection<StockRow> Results { get; } = [];
    public ObservableCollection<CartItem> Cart { get; } = [];
    public ObservableCollection<RecentSale> RecentSales { get; } = [];

    public decimal Total => Cart.Sum(c => c.Subtotal);
    public decimal? ChangePreview => CashReceived is { } cash && cash >= Total && Total > 0 ? cash - Total : null;
    public bool NeedsPrescription => Cart.Any(c => c.Product.RequiresPrescription);

    public async Task ActivateAsync()
    {
        await LoadProductsAsync();
        await LoadRecentAsync();
    }

    partial void OnSearchChanged(string value) => _ = LoadProductsAsync();

    private async Task LoadProductsAsync()
    {
        var overview = await operations.RunAsync<InventoryQueries, InventoryOverview>(q => q.GetOverviewAsync(Search));
        Results.Clear();
        foreach (var row in overview.Stock) Results.Add(row);
    }

    private async Task LoadRecentAsync()
    {
        var sales = await operations.QueryAsync(db => db.Sales.OrderByDescending(s => s.CreatedAt).Take(8).ToListAsync());
        RecentSales.Clear();
        foreach (var s in sales)
            RecentSales.Add(new RecentSale(s.Id, s.ReceiptNumber, s.CreatedAt, s.Total, s.Status.ToString()));
    }

    [RelayCommand]
    private void Add(StockRow row)
    {
        Error = "";
        var item = Cart.FirstOrDefault(c => c.Product.ProductId == row.ProductId);
        var wanted = (item?.Quantity ?? 0) + 1;
        if (wanted > row.Stock)
        {
            Error = $"Stock insuficiente de {row.Name}: hay {row.Stock} unidades vigentes (RF-039).";
            return;
        }
        if (item is null) Cart.Add(new CartItem(row, CartChanged));
        else item.Quantity = wanted;
        CartChanged();
    }

    [RelayCommand]
    private void Increase(CartItem item) => Add(item.Product);

    [RelayCommand]
    private void Decrease(CartItem item)
    {
        if (item.Quantity > 1) item.Quantity--;
        else Remove(item);
    }

    [RelayCommand]
    private void Remove(CartItem item)
    {
        Cart.Remove(item);
        CartChanged();
    }

    [RelayCommand]
    private Task ConfirmAsync() => RunAsync(async () =>
    {
        var command = new RegisterSaleCommand(
            Cart.Select(c => new SaleItemRequest(c.Product.ProductId, c.Quantity)).ToList(),
            CashReceived ?? 0, PrescriptionNumber, 0, CustomerName, CustomerTaxId);
        var receipt = await operations.RunAsync<RegisterSaleHandler, SaleReceipt>(h => h.HandleAsync(command),
            changesData: true);

        Receipt = Format(receipt);
        Cart.Clear();
        CashReceived = null;
        PrescriptionNumber = CustomerName = CustomerTaxId = "";
        CartChanged();
        await ActivateAsync();
    }, "Venta registrada en el puesto. Se sincronizará con el servidor automáticamente.");

    [RelayCommand]
    private Task VoidAsync(RecentSale sale) => RunAsync(async () =>
    {
        await operations.RunAsync<VoidSaleHandler>(h => h.HandleAsync(new VoidSaleCommand(sale.Id, VoidReason)),
            changesData: true);
        VoidReason = "";
        await ActivateAsync();
    }, $"Venta {sale.ReceiptNumber} anulada; el stock volvió a sus lotes.");

    private void CartChanged()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ChangePreview));
        OnPropertyChanged(nameof(NeedsPrescription));
    }

    /// <summary>Recibo interno sin validez fiscal (RF-049).</summary>
    private static string Format(SaleReceipt r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("FARMACIA · RECIBO SIN VALOR FISCAL");
        sb.AppendLine($"N.º {r.ReceiptNumber}   {r.CreatedAt.LocalDateTime:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Cajero: {r.CashierName}");
        sb.AppendLine(new string('─', 40));
        foreach (var l in r.Lines)
            sb.AppendLine($"{l.Quantity,3} × {l.ProductName}\n      lote {l.LotNumber}   Bs {l.UnitPrice * l.Quantity,8:N2}");
        sb.AppendLine(new string('─', 40));
        sb.AppendLine($"TOTAL            Bs {r.Total,10:N2}");
        sb.AppendLine($"Efectivo         Bs {r.CashReceived,10:N2}");
        sb.AppendLine($"Cambio           Bs {r.Change,10:N2}");
        return sb.ToString();
    }
}
