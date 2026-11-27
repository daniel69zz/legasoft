using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// SCRUM-28 · HU-24 (RF-084 a RF-087): registro, modificación y búsqueda de clientes con su historial de
/// compras. Los clientes viven en el servidor, por eso la pantalla requiere conexión.
/// </summary>
public sealed partial class CustomersViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle))] private CustomerDto? _selected;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _taxId = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";

    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<CustomerPurchaseDto> Purchases { get; } = [];

    public bool IsEditing => Selected is not null;
    public string FormTitle => IsEditing ? "Modificar cliente (RF-085)" : "Registrar cliente (RF-084)";

    public Task ActivateAsync() => RunAsync(LoadAsync);

    partial void OnSearchChanged(string value) => _ = RunAsync(LoadAsync);

    partial void OnSelectedChanged(CustomerDto? value)
    {
        Purchases.Clear();
        if (value is null) return;
        (Name, TaxId, Phone, Email) = (value.Name, value.TaxId, value.Phone, value.Email);
        _ = RunAsync(async () =>
        {
            var purchases = await api.GetCustomerPurchasesAsync(value.Id);
            Purchases.Clear();
            foreach (var p in purchases) Purchases.Add(p);
            if (purchases.Count == 0) Info = $"{value.Name} todavía no tiene compras registradas con su NIT/CI.";
        });
    }

    private async Task LoadAsync()
    {
        var selectedId = Selected?.Id;
        var customers = await api.SearchCustomersAsync(Search);
        Customers.Clear();
        foreach (var c in customers) Customers.Add(c);
        if (selectedId is { } id) Selected = Customers.FirstOrDefault(c => c.Id == id);
    }

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        var request = new CustomerRequest(Name, TaxId, Phone, Email);
        if (Selected is { } current)
        {
            await api.UpdateCustomerAsync(current.Id, request);
            Info = $"Cliente {Name} actualizado.";
        }
        else
        {
            try
            {
                Selected = await api.CreateCustomerAsync(request);
                Info = $"Cliente {Name} registrado.";
            }
            catch (ApiException e) when (e.Status == 409)
            {
                // RF-084: el NIT/CI ya existe; se muestra el cliente registrado.
                Search = TaxId.Trim();
                await LoadAsync();
                Selected = Customers.FirstOrDefault(c => c.TaxId == TaxId.Trim());
                Error = $"Ya existe un cliente con el NIT/CI {TaxId.Trim()}: se muestra el registrado.";
                return;
            }
        }
        await LoadAsync();
    });

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Name = TaxId = Phone = Email = "";
        Error = Info = "";
    }
}
