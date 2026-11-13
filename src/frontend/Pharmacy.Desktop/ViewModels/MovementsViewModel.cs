using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// SCRUM-123 · HU-06.4 (RF-019): historial general de movimientos consolidado en el servidor, con filtros por
/// período, tipo y producto. Requiere conexión; el historial de un producto en el puesto está en Inventario.
/// </summary>
public sealed partial class MovementsViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    public static readonly string[] MovementTypes = ["Todos", "Compra", "Venta", "Ajuste", "Entrada", "Salida", "Retiro"];

    [ObservableProperty] private DateTime? _from = DateTime.Today.AddDays(-6);
    [ObservableProperty] private DateTime? _to = DateTime.Today;
    [ObservableProperty] private string _type = MovementTypes[0];
    [ObservableProperty] private string _search = "";

    private List<InventoryMovementDto> _all = [];

    public string[] Types => MovementTypes;
    public ObservableCollection<InventoryMovementDto> Movements { get; } = [];
    public int TotalUnits => Movements.Sum(m => m.Quantity);

    public Task ActivateAsync() => RunAsync(LoadAsync);

    [RelayCommand]
    private Task FilterAsync() => RunAsync(LoadAsync);

    partial void OnSearchChanged(string value) => ApplySearch();

    private async Task LoadAsync()
    {
        var from = DateOnly.FromDateTime(From ?? DateTime.Today.AddDays(-6));
        var to = DateOnly.FromDateTime(To ?? DateTime.Today);
        _all = await api.GetMovementsAsync(from, to, Type == MovementTypes[0] ? null : Type);
        ApplySearch();
        if (_all.Count == 0) Info = "No hay movimientos con esos filtros.";
    }

    private void ApplySearch()
    {
        Movements.Clear();
        foreach (var m in _all.Where(m => string.IsNullOrWhiteSpace(Search)
                                          || m.ProductName.Contains(Search, StringComparison.OrdinalIgnoreCase)
                                          || m.ProductCode.Contains(Search, StringComparison.OrdinalIgnoreCase)))
            Movements.Add(m);
        OnPropertyChanged(nameof(TotalUnits));
    }
}
