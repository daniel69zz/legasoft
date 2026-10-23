using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Dashboard gerencial consolidado en el servidor (RF-073, RF-074). Requiere conexión.</summary>
public sealed partial class ReportsViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    [ObservableProperty] private DashboardSummary? _summary;

    public ObservableCollection<SalesByCashierRow> SalesByCashier { get; } = [];
    public ObservableCollection<AuditEntryDto> Audit { get; } = [];

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var summary = await api.GetDashboardAsync();
        List<AuditEntryDto> audit;
        try
        {
            audit = await api.GetAuditAsync();
        }
        catch (ApiException e) when (e.Status == 403)
        {
            audit = []; // la auditoría es solo para algunos roles
        }

        // Se reemplaza todo junto, después de las llamadas, para que dos cargas seguidas no mezclen filas.
        Summary = summary;
        SalesByCashier.Clear();
        foreach (var row in summary.SalesByCashier.OrderByDescending(r => r.Day)) SalesByCashier.Add(row);
        Audit.Clear();
        foreach (var a in audit) Audit.Add(a);
    });
}
