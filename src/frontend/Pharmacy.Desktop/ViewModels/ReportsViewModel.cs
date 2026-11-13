using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Dashboard gerencial consolidado en el servidor (RF-073, RF-074). Requiere conexión.</summary>
public sealed partial class ReportsViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    [ObservableProperty] private DashboardSummary? _summary;

    // HU-20.1 (RF-075): filtros de auditoría.
    [ObservableProperty] private DateTime? _auditFrom = DateTime.Today.AddDays(-6);
    [ObservableProperty] private DateTime? _auditTo = DateTime.Today;
    [ObservableProperty] private string _auditUser = "";
    [ObservableProperty] private string _auditAction = "";
    [ObservableProperty] private bool _canAudit = true;

    // HU-18.1 (RF-105): período a exportar.
    [ObservableProperty] private DateTime? _exportFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime? _exportTo = DateTime.Today;
    [ObservableProperty] private string _exportedPath = "";

    /// <summary>Carpeta donde se guardan los .xlsx; se puede cambiar en las pruebas.</summary>
    public static string ExportFolder { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    public ObservableCollection<SalesByCashierRow> SalesByCashier { get; } = [];
    public ObservableCollection<AuditEntryDto> Audit { get; } = [];

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var summary = await api.GetDashboardAsync();
        var audit = await SearchAuditAsync();

        // Se reemplaza todo junto, después de las llamadas, para que dos cargas seguidas no mezclen filas.
        Summary = summary;
        SalesByCashier.Clear();
        foreach (var row in summary.SalesByCashier.OrderByDescending(r => r.Day)) SalesByCashier.Add(row);
        Audit.Clear();
        foreach (var a in audit) Audit.Add(a);
    });

    private async Task<List<AuditEntryDto>> SearchAuditAsync()
    {
        try
        {
            var audit = await api.SearchAuditAsync(ToDate(AuditFrom), ToDate(AuditTo), AuditUser, AuditAction);
            CanAudit = true;
            return audit;
        }
        catch (ApiException e) when (e.Status == 403)
        {
            CanAudit = false; // la auditoría es solo para algunos roles
            return [];
        }
    }

    [RelayCommand]
    private Task FilterAuditAsync() => RunAsync(async () =>
    {
        var audit = await SearchAuditAsync();
        Audit.Clear();
        foreach (var a in audit) Audit.Add(a);
        if (audit.Count == 0) Info = "No hay registros de auditoría con esos filtros.";
    });

    /// <summary>HU-18.1 (RF-105): guarda el .xlsx del período en la carpeta de descargas.</summary>
    [RelayCommand]
    private Task ExportAsync() => RunAsync(async () =>
    {
        var file = await api.ExportAsync(ToDate(ExportFrom) ?? DateOnly.FromDateTime(DateTime.Today),
            ToDate(ExportTo) ?? DateOnly.FromDateTime(DateTime.Today));
        Directory.CreateDirectory(ExportFolder);
        var path = Path.Combine(ExportFolder, file.FileName);
        await File.WriteAllBytesAsync(path, file.Content);
        ExportedPath = path;
        Info = file.IsEmpty
            ? $"No hay ventas, egresos ni cierres en el período. Se guardó el archivo vacío en {path}."
            : $"Reporte exportado a {path}.";
    });

    private static DateOnly? ToDate(DateTime? value) => value is { } d ? DateOnly.FromDateTime(d) : null;
}
