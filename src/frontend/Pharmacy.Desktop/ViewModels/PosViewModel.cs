using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Application.Inventory;
using Pharmacy.Application.Sales;
using Pharmacy.Desktop.Data;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Identity;
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

/// <summary>
/// Punto de venta (RF-037 a RF-049): carrito, FEFO, receta y cambio. En el Sprint 3 suma la receta completa
/// (SCRUM-29 · HU-25), el descuento con permiso (SCRUM-30 · HU-27), el faltante (SCRUM-22 · HU-12), la
/// búsqueda de clientes (SCRUM-28 · HU-24), los recibos en PDF (SCRUM-24 · HU-14) y la factura simulada
/// (SCRUM-27 · HU-17).
/// </summary>
public sealed partial class PosViewModel(Operations operations, ApiClient api, Session session)
    : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ChangePreview), nameof(Missing), nameof(HasMissing))]
    private decimal? _cashReceived;
    [ObservableProperty] private string _prescriptionNumber = "";
    [ObservableProperty] private string _prescriptionDoctor = "";
    [ObservableProperty] private string _prescriptionPatient = "";
    [ObservableProperty] private DateTime? _prescriptionDate = DateTime.Today;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalToPay), nameof(ChangePreview), nameof(Missing), nameof(HasMissing))]
    private decimal? _discount;
    [ObservableProperty] private DateTime? _receiptsFrom = DateTime.Today;
    [ObservableProperty] private DateTime? _receiptsTo = DateTime.Today;
    [ObservableProperty] private string _lastFile = "";
    [ObservableProperty] private string _customerName = "";
    [ObservableProperty] private string _customerTaxId = "";
    [ObservableProperty] private string _receipt = "";
    [ObservableProperty] private string _voidReason = "";

    public ObservableCollection<StockRow> Results { get; } = [];
    public ObservableCollection<CartItem> Cart { get; } = [];
    public ObservableCollection<RecentSale> RecentSales { get; } = [];

    public decimal Total => Cart.Sum(c => c.Subtotal);

    /// <summary>SCRUM-30 · HU-27: total con el descuento aplicado.</summary>
    public decimal TotalToPay => Math.Max(0, Total - (Discount ?? 0));

    public decimal? ChangePreview =>
        CashReceived is { } cash && cash >= TotalToPay && TotalToPay > 0 ? cash - TotalToPay : null;

    /// <summary>SCRUM-22 · HU-12 (RF-044): cuánto falta cuando el efectivo no alcanza.</summary>
    public decimal? Missing => CashReceived is { } cash && cash > 0 && cash < TotalToPay ? TotalToPay - cash : null;

    public bool HasMissing => Missing is not null;
    public bool NeedsPrescription => Cart.Any(c => c.Product.RequiresPrescription);

    /// <summary>SCRUM-30 · HU-27 (RF-096): el campo de descuento solo aparece con el permiso.</summary>
    public bool CanDiscount => RolePermissions.Has(session.Role, Permission.Descuentos);

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
        // SQLite no ordena DateTimeOffset en SQL: se ordena en memoria.
        var sales = (await operations.QueryAsync(db => db.Sales.ToListAsync()))
            .OrderByDescending(s => s.CreatedAt).Take(8).ToList();
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
            CashReceived ?? 0, PrescriptionNumber, Discount ?? 0, CustomerName, CustomerTaxId,
            PrescriptionDoctor, PrescriptionPatient,
            PrescriptionDate is { } d ? DateOnly.FromDateTime(d) : null);
        var receipt = await operations.RunAsync<RegisterSaleHandler, SaleReceipt>(h => h.HandleAsync(command),
            changesData: true);

        Receipt = Receipts.Text(await CompanyAsync(), receipt);
        Cart.Clear();
        CashReceived = null;
        Discount = null;
        PrescriptionNumber = PrescriptionDoctor = PrescriptionPatient = CustomerName = CustomerTaxId = "";
        PrescriptionDate = DateTime.Today;
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

    /// <summary>SCRUM-28 · HU-24 (RF-086): completa el nombre del cliente a partir de su NIT o CI (requiere conexión).</summary>
    [RelayCommand]
    private Task LookupCustomerAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(CustomerTaxId)) throw new Pharmacy.Domain.Common.DomainException(
            "Escriba el NIT o CI del cliente para buscarlo.");
        var found = (await api.SearchCustomersAsync(CustomerTaxId.Trim()))
            .FirstOrDefault(c => c.TaxId == CustomerTaxId.Trim());
        if (found is null)
        {
            Info = "No hay un cliente registrado con ese NIT/CI. Puede registrarlo en «Clientes».";
            return;
        }
        CustomerName = found.Name;
        Info = $"Cliente: {found.Name}.";
    });

    /// <summary>SCRUM-24 · HU-14 (RF-050): descarga el recibo de una venta en PDF; funciona sin conexión.</summary>
    [RelayCommand]
    private Task DownloadReceiptAsync(RecentSale recent) => RunAsync(async () =>
    {
        var sale = await operations.QueryAsync(db => db.Sales.SingleAsync(s => s.Id == recent.Id));
        var pdf = Receipts.Pdf(await CompanyAsync(), SalePayloads.Receipt(sale));
        LastFile = await Downloads.SaveAsync(pdf, $"recibo-{sale.ReceiptNumber}.pdf");
        Info = $"Recibo guardado en {LastFile}.";
    });

    /// <summary>SCRUM-24 · HU-14 (RF-051): recibos del período en un solo PDF (requiere conexión).</summary>
    [RelayCommand]
    private Task DownloadReceiptsAsync() => RunAsync(async () =>
    {
        var from = DateOnly.FromDateTime(ReceiptsFrom ?? DateTime.Today);
        var to = DateOnly.FromDateTime(ReceiptsTo ?? DateTime.Today);
        var file = await api.DownloadReceiptsAsync(from, to, session.DeviceId);
        LastFile = await Downloads.SaveAsync(file.Content, file.FileName);
        Info = $"Recibos del período guardados en {LastFile}.";
    });

    /// <summary>
    /// SCRUM-27 · HU-17 (RF-063 a RF-067): factura simulada de una venta ya sincronizada, con el cliente del
    /// formulario o el de la venta. Requiere conexión porque el número correlativo lo asigna el servidor.
    /// </summary>
    [RelayCommand]
    private Task InvoiceAsync(RecentSale recent) => RunAsync(async () =>
    {
        var sale = await operations.QueryAsync(db => db.Sales.SingleAsync(s => s.Id == recent.Id));
        var name = string.IsNullOrWhiteSpace(CustomerName) ? sale.CustomerName : CustomerName;
        var taxId = string.IsNullOrWhiteSpace(CustomerTaxId) ? sale.CustomerTaxId : CustomerTaxId;
        var invoice = await api.IssueInvoiceAsync(sale.Id, new InvoiceRequest(name, taxId));
        var file = await api.DownloadInvoiceAsync(invoice.Number);
        LastFile = await Downloads.SaveAsync(file.Content, file.FileName);
        Info = $"Factura simulada N.º {invoice.Number:D6} para {invoice.CustomerName} guardada en {LastFile}.";
    });

    private Task<CompanyProfileDto> CompanyAsync() =>
        operations.QueryAsync(db => new LocalStore(db).GetCompanyAsync());

    private void CartChanged()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalToPay));
        OnPropertyChanged(nameof(ChangePreview));
        OnPropertyChanged(nameof(Missing));
        OnPropertyChanged(nameof(HasMissing));
        OnPropertyChanged(nameof(NeedsPrescription));
    }
}
