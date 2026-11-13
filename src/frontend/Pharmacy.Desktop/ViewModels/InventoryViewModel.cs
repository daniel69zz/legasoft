using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Inventory;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Identity;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Desktop.ViewModels;

public sealed record MovementTypeOption(StockMovementType Type, string Label, string Hint)
{
    public override string ToString() => Label;
}

public sealed record LotOption(Guid Id, string LotNumber, DateOnly ExpirationDate, int Quantity)
{
    public override string ToString() => $"{LotNumber} · vence {ExpirationDate:dd/MM/yyyy} · {Quantity} u.";
}

/// <summary>
/// Consulta de stock vendible, ubicación, lotes por vencer y stock mínimo (RF-012, RF-013, RF-018, RF-022)
/// y, para el producto elegido, entradas, salidas y ajustes manuales con su historial (HU-06.3).
/// </summary>
public sealed partial class InventoryViewModel(Operations operations, Session session) : ViewModelBase, IActivatable
{
    public static readonly MovementTypeOption[] MovementTypes =
    [
        new(StockMovementType.Ajuste, "Ajuste", "Diferencia con signo: −3 resta tres unidades, 5 suma cinco."),
        new(StockMovementType.Entrada, "Entrada", "Unidades que ingresan al lote (por ejemplo, una devolución)."),
        new(StockMovementType.Salida, "Salida", "Unidades que salen del lote (por ejemplo, una muestra)."),
        new(StockMovementType.Retiro, "Retiro (vencido o dañado)", "Baja de unidades vencidas o dañadas; acepta lotes vencidos (RF-025)."),
    ];

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private StockRow? _selected;
    [ObservableProperty] private LotOption? _lot;
    [ObservableProperty] private MovementTypeOption _movementType = MovementTypes[0];
    [ObservableProperty] private decimal? _quantity;
    [ObservableProperty] private string _reason = "";

    public ObservableCollection<StockRow> Rows { get; } = [];
    public ObservableCollection<ExpiryAlert> ExpiryAlerts { get; } = [];
    public ObservableCollection<LowStockAlert> LowStockAlerts { get; } = [];
    public ObservableCollection<LotOption> Lots { get; } = [];
    public ObservableCollection<StockMovementRow> Movements { get; } = [];

    public MovementTypeOption[] Types => MovementTypes;

    /// <summary>El Gerente General consulta el inventario, pero no lo modifica.</summary>
    public bool CanAdjust => RolePermissions.Has(session.Role, Permission.Inventario)
                             && session.Role != Roles.GerenteGeneral;

    public bool HasSelection => Selected is not null;

    partial void OnSearchChanged(string value) => _ = ActivateAsync();

    private Guid? _detailProductId;

    partial void OnSelectedChanged(StockRow? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is null || value.ProductId == _detailProductId) return;
        _detailProductId = value.ProductId;
        Quantity = null;
        Reason = "";
        _ = ShowDetailAsync(value.ProductId);
    }

    public Task ActivateAsync() => RunAsync(LoadOverviewAsync);

    private async Task LoadOverviewAsync()
    {
        var overview = await operations.RunAsync<InventoryQueries, InventoryOverview>(q => q.GetOverviewAsync(Search));
        var selectedId = Selected?.ProductId;
        Replace(Rows, overview.Stock);
        Replace(ExpiryAlerts, overview.ExpiryAlerts);
        Replace(LowStockAlerts, overview.LowStockAlerts);
        Selected = Rows.FirstOrDefault(r => r.ProductId == selectedId);
    }

    // Carga el detalle sin RunAsync para no borrar el mensaje del último movimiento registrado.
    private async Task ShowDetailAsync(Guid productId)
    {
        try
        {
            await LoadDetailAsync(productId);
        }
        catch (Exception e)
        {
            Error = e.Message;
        }
    }

    private async Task LoadDetailAsync(Guid productId)
    {
        var lots = await operations.RunAsync<InventoryQueries, IReadOnlyList<Lot>>(q => q.GetLotsAsync(productId));
        var movements = await operations.RunAsync<InventoryQueries, IReadOnlyList<StockMovementRow>>(
            q => q.GetMovementsAsync(productId));
        var lotId = Lot?.Id;
        Replace(Lots, lots.Select(l => new LotOption(l.Id, l.LotNumber, l.ExpirationDate, l.Quantity)));
        Lot = Lots.FirstOrDefault(l => l.Id == lotId) ?? Lots.FirstOrDefault();
        Replace(Movements, movements);
    }

    /// <summary>Registra el movimiento en el puesto; viaja al servidor por el Outbox.</summary>
    [RelayCommand]
    private Task AdjustAsync()
    {
        var product = Selected;
        var lot = Lot;
        return RunAsync(async () =>
        {
            if (product is null) throw new Domain.Common.DomainException("Elija un producto del listado.");
            if (lot is null) throw new Domain.Common.DomainException("El producto no tiene lotes con existencia.");
            var row = await operations.RunAsync<AdjustStockHandler, StockMovementRow>(h => h.HandleAsync(
                new AdjustStockCommand(product.ProductId, lot.Id, MovementType.Type, (int)(Quantity ?? 0), Reason)),
                changesData: true);
            Quantity = null;
            Reason = "";
            await LoadOverviewAsync();
            await LoadDetailAsync(product.ProductId);
            Info = $"{row.Type} de {row.Quantity:+0;-0} u. en el lote {row.LotNumber} registrado.";
        });
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}
