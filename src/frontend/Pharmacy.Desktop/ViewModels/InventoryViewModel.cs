using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Pharmacy.Application.Inventory;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Consulta de stock vendible, ubicación, lotes por vencer y stock mínimo (RF-012, RF-013, RF-018, RF-022).</summary>
public sealed partial class InventoryViewModel(Operations operations) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _search = "";

    public ObservableCollection<StockRow> Rows { get; } = [];
    public ObservableCollection<ExpiryAlert> ExpiryAlerts { get; } = [];
    public ObservableCollection<LowStockAlert> LowStockAlerts { get; } = [];

    partial void OnSearchChanged(string value) => _ = ActivateAsync();

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var overview = await operations.RunAsync<InventoryQueries, InventoryOverview>(q => q.GetOverviewAsync(Search));
        Replace(Rows, overview.Stock);
        Replace(ExpiryAlerts, overview.ExpiryAlerts);
        Replace(LowStockAlerts, overview.LowStockAlerts);
    });

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}
