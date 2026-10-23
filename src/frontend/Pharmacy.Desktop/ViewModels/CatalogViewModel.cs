using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// HU-05: catálogo único de medicamentos (RF-009 a RF-012). Se administra en línea contra el
/// Business Service; los puestos reciben los cambios en la siguiente sincronización.
/// </summary>
public sealed partial class CatalogViewModel(ApiClient api, SyncWorker sync) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private ProductDto? _selected;

    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _activeIngredient = "";
    [ObservableProperty] private string _concentration = "";
    [ObservableProperty] private string _presentation = "";
    [ObservableProperty] private string _laboratory = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private bool _requiresPrescription;
    [ObservableProperty] private decimal? _salePrice;
    [ObservableProperty] private decimal? _minimumStock = 0;
    [ObservableProperty] private string _location = "";

    public ObservableCollection<ProductDto> Products { get; } = [];

    public bool IsEditing => Selected is not null;
    public string FormTitle => IsEditing ? "Modificar medicamento (RF-010)" : "Registrar medicamento (RF-009)";

    public Task ActivateAsync() => RunAsync(LoadAsync);

    partial void OnSearchChanged(string value) => _ = ActivateAsync();

    partial void OnSelectedChanged(ProductDto? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(FormTitle));
        if (value is null) return;
        Code = value.Code;
        Name = value.Name;
        ActiveIngredient = value.ActiveIngredient;
        Concentration = value.Concentration;
        Presentation = value.Presentation;
        Laboratory = value.Laboratory;
        Category = value.Category;
        RequiresPrescription = value.RequiresPrescription;
        SalePrice = value.SalePrice;
        MinimumStock = value.MinimumStock;
        Location = value.Location;
    }

    private async Task LoadAsync()
    {
        var products = await api.SearchCatalogAsync(Search);
        Products.Clear();
        foreach (var p in products) Products.Add(p);
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Code = Name = ActiveIngredient = Concentration = Presentation = Laboratory = Category = Location = "";
        RequiresPrescription = false;
        SalePrice = null;
        MinimumStock = 0;
        Error = Info = "";
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        var editing = Selected;
        return RunAsync(async () =>
        {
            var request = new SaveProductRequest(Code, Name, ActiveIngredient, Concentration, Presentation, Laboratory,
                Category, RequiresPrescription, SalePrice ?? 0, (int)(MinimumStock ?? 0), Location);
            var saved = editing is null
                ? await api.CreateProductAsync(request)
                : await api.UpdateProductAsync(editing.Id, request);
            await LoadAsync();
            Selected = Products.FirstOrDefault(p => p.Id == saved.Id);
            sync.Wake();
        }, editing is null ? "Medicamento registrado." : "Cambios guardados.");
    }

    [RelayCommand]
    private Task ToggleAsync(ProductDto product) => RunAsync(async () =>
    {
        await api.SetProductActiveAsync(product.Id, !product.IsActive);
        await LoadAsync();
        sync.Wake();
    }, product.IsActive ? $"{product.Code} desactivado: ya no se ofrece en ventas." : $"{product.Code} activado.");
}
