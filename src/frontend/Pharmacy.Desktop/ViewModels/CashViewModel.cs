using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Cash;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Cash;

namespace Pharmacy.Desktop.ViewModels;

public sealed partial class DenominationEntry(decimal value, Action changed) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal? _count;

    public decimal Value { get; } = value;
    public string Label => CashDenominations.IsBill(Value) ? $"Billete Bs {Value:0}" : $"Moneda Bs {Value:0.##}";
    public decimal Subtotal => Value * (Count ?? 0);

    partial void OnCountChanged(decimal? value) => changed();
}

public sealed record MovementRow(DateTimeOffset OccurredAt, string Type, string Detail, decimal SignedAmount);

/// <summary>Caja del puesto (RF-052 a RF-060, RF-106): apertura, egresos y cierre por denominación.</summary>
public sealed partial class CashViewModel : ViewModelBase, IActivatable
{
    public static readonly string[] ExpenseCategories = ["Insumos", "Servicios básicos", "Transporte", "Limpieza", "Otros"];

    private readonly Operations _operations;

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private decimal? _openingAmount = 200;
    [ObservableProperty] private decimal _openingShown;
    [ObservableProperty] private decimal _salesTotal;
    [ObservableProperty] private decimal _expensesTotal;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Difference))] private decimal _expected;
    [ObservableProperty] private decimal? _expenseAmount;
    [ObservableProperty] private string _expenseReason = "";
    [ObservableProperty] private string _expenseCategory = ExpenseCategories[0];
    [ObservableProperty] private string _justification = "";
    [ObservableProperty] private string _closingReport = "";

    public CashViewModel(Operations operations)
    {
        _operations = operations;
        foreach (var d in CashDenominations.All) Denominations.Add(new DenominationEntry(d, CountChanged));
    }

    public string[] Categories => ExpenseCategories;
    public ObservableCollection<DenominationEntry> Denominations { get; } = [];
    public ObservableCollection<MovementRow> Movements { get; } = [];
    public decimal Counted => Denominations.Sum(d => d.Subtotal);
    public decimal Difference => Counted - Expected;

    public Task ActivateAsync() => RunAsync(LoadAsync);

    private async Task LoadAsync()
    {
        var session = await _operations.RunAsync<CashHandlers, CashSession?>(h => h.GetOpenAsync());
        IsOpen = session is not null;
        Movements.Clear();
        if (session is null) return;

        OpeningShown = session.OpeningAmount;
        SalesTotal = session.SalesTotal;
        ExpensesTotal = session.ExpensesTotal;
        Expected = session.ExpectedAmount;
        foreach (var m in session.Movements.OrderByDescending(m => m.OccurredAt))
            Movements.Add(new MovementRow(m.OccurredAt, m.Type.ToString(),
                m.Type == CashMovementType.Egreso ? $"{m.Category}: {m.Reason}" : m.Reason, m.SignedAmount));
    }

    [RelayCommand]
    private Task OpenAsync() => RunAsync(async () =>
    {
        await _operations.RunAsync<CashHandlers, CashSession>(h => h.OpenAsync(OpeningAmount ?? 0), changesData: true);
        ClosingReport = "";
        await LoadAsync();
    }, "Caja abierta.");

    [RelayCommand]
    private Task RegisterExpenseAsync() => RunAsync(async () =>
    {
        await _operations.RunAsync<CashHandlers, CashMovement>(
            h => h.RegisterExpenseAsync(ExpenseAmount ?? 0, ExpenseReason, ExpenseCategory), changesData: true);
        ExpenseAmount = null;
        ExpenseReason = "";
        await LoadAsync();
    }, "Egreso registrado.");

    [RelayCommand]
    private Task CloseAsync() => RunAsync(async () =>
    {
        var units = Denominations.Where(d => d.Count > 0).ToDictionary(d => d.Value, d => (int)d.Count!.Value);
        var report = await _operations.RunAsync<CashHandlers, CashSessionClosedPayload>(
            h => h.CloseAsync(units, Justification), changesData: true);
        ClosingReport = Format(report);
        foreach (var d in Denominations) d.Count = null;
        Justification = "";
        await LoadAsync();
    }, "Caja cerrada. El reporte de cierre se sincronizará con el servidor.");

    private void CountChanged()
    {
        OnPropertyChanged(nameof(Counted));
        OnPropertyChanged(nameof(Difference));
    }

    /// <summary>Reporte de cierre de caja (RF-060).</summary>
    private static string Format(CashSessionClosedPayload r) => $"""
        REPORTE DE CIERRE DE CAJA · {r.DeviceId}
        Cajero: {r.CashierName}
        Turno: {r.OpenedAt.LocalDateTime:dd/MM HH:mm} – {r.ClosedAt.LocalDateTime:HH:mm}
        ────────────────────────────────────
        Monto inicial      Bs {r.OpeningAmount,10:N2}
        Ventas           + Bs {r.SalesTotal,10:N2}
        Egresos          − Bs {r.ExpensesTotal,10:N2}
        Esperado           Bs {r.ExpectedAmount,10:N2}
        Contado            Bs {r.CountedAmount,10:N2}
        Diferencia         Bs {r.Difference,10:N2}
        {(string.IsNullOrEmpty(r.Justification) ? "" : $"Justificación: {r.Justification}")}
        """;
}
