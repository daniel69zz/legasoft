using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// HU-08: proveedores (RF-026 a RF-029). Se administran en línea contra el Business Service;
/// al elegir uno se muestran los productos que suministró y su historial de compras.
/// </summary>
public sealed partial class SuppliersViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private SupplierDto? _selected;

    [ObservableProperty] private string _taxId = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _contactName = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _address = "";

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<SupplierProductDto> Products { get; } = [];
    public ObservableCollection<PurchaseDto> History { get; } = [];

    public bool IsEditing => Selected is not null;
    public string FormTitle => IsEditing ? "Modificar proveedor (RF-027)" : "Registrar proveedor (RF-026)";

    public Task ActivateAsync() => RunAsync(LoadAsync);

    partial void OnSearchChanged(string value) => _ = ActivateAsync();

    partial void OnSelectedChanged(SupplierDto? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        Products.Clear();
        History.Clear();
        if (value is null) return;
        TaxId = value.TaxId;
        Name = value.Name;
        ContactName = value.ContactName;
        Phone = value.Phone;
        Email = value.Email;
        Address = value.Address;
        _ = RunAsync(() => LoadDetailAsync(value.Id));
    }

    private async Task LoadAsync()
    {
        // Al vaciar la lista el ListBox anula la selección: se restaura para que el formulario
        // no quede con los datos de un proveedor y el título "Registrar".
        var suppliers = await api.GetSuppliersAsync(Search);
        var selectedId = Selected?.Id; // después de la espera: otra carga pudo cambiarla
        Suppliers.Clear();
        foreach (var s in suppliers) Suppliers.Add(s);
        var restored = Suppliers.FirstOrDefault(s => s.Id == selectedId);
        if (restored is null && selectedId is not null) New();
        else Selected = restored;
    }

    private async Task LoadDetailAsync(Guid id)
    {
        var products = await api.GetSupplierProductsAsync(id);
        var history = await api.GetSupplierPurchasesAsync(id);
        Products.Clear();
        foreach (var p in products) Products.Add(p);
        History.Clear();
        foreach (var h in history) History.Add(h);
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        TaxId = Name = ContactName = Phone = Email = Address = "";
        Error = Info = "";
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        var editing = Selected;
        return RunAsync(async () =>
        {
            var request = new SaveSupplierRequest(TaxId, Name, ContactName, Phone, Email, Address);
            var saved = editing is null
                ? await api.CreateSupplierAsync(request)
                : await api.UpdateSupplierAsync(editing.Id, request);
            await LoadAsync();
            Selected = Suppliers.FirstOrDefault(s => s.Id == saved.Id);
        }, editing is null ? "Proveedor registrado: ya se puede usar en compras." : "Cambios guardados.");
    }

    [RelayCommand]
    private Task ToggleAsync(SupplierDto supplier) => RunAsync(async () =>
    {
        await api.SetSupplierActiveAsync(supplier.Id, !supplier.IsActive);
        await LoadAsync();
    }, supplier.IsActive ? $"{supplier.Name} desactivado: no se ofrece en compras nuevas." : $"{supplier.Name} activado.");
}
