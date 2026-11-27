using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Documents;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

public sealed record ReportOption(string Kind, string Label)
{
    public override string ToString() => Label;
}

public sealed record GroupingOption(string Value, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// Dashboard gerencial consolidado en el servidor (RF-073, RF-074). Requiere conexión. En el Sprint 3 suma los
/// reportes del período y el cierre diario (SCRUM-31 · HU-18), el comparativo por semana o mes
/// (SCRUM-32 · HU-19) y el resumen por sucursal (SCRUM-34 · HU-29).
/// </summary>
public sealed partial class ReportsViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    public static readonly IReadOnlyList<GroupingOption> Groupings =
    [
        new(SalesGrouping.Day, "Por día (últimos 7 días)"),
        new(SalesGrouping.Week, "Por semana (últimas 8)"),
        new(SalesGrouping.Month, "Por mes (últimos 6)"),
    ];

    public static readonly IReadOnlyList<ReportOption> ReportOptions =
        ReportKinds.All.Select(k => new ReportOption(k.Kind, k.Label)).ToList();

    [ObservableProperty] private DashboardSummary? _summary;
    [ObservableProperty] private GroupingOption _grouping = Groupings[0];

    // SCRUM-31 · HU-18: reporte del período.
    [ObservableProperty] private ReportOption _reportKind = ReportOptions[0];
    [ObservableProperty] private DateTime? _reportFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime? _reportTo = DateTime.Today;
    [ObservableProperty] private string _branch = "";
    [ObservableProperty] private ReportTableDto? _report;
    [ObservableProperty] private string _reportPreview = "";
    [ObservableProperty] private string _branchSummary = "";

    public IReadOnlyList<GroupingOption> GroupingChoices => Groupings;
    public IReadOnlyList<ReportOption> ReportChoices => ReportOptions;

    /// <summary>El cierre diario usa una sola fecha: la de «hasta».</summary>
    public bool IsRange => ReportKind.Kind != ReportKinds.DailyClosing;

    partial void OnReportKindChanged(ReportOption value) => OnPropertyChanged(nameof(IsRange));

    partial void OnGroupingChanged(GroupingOption value) => _ = RunAsync(LoadDashboardAsync);

    // SCRUM-128 · HU-20.1 (RF-075): filtros de auditoría.
    [ObservableProperty] private DateTime? _auditFrom = DateTime.Today.AddDays(-6);
    [ObservableProperty] private DateTime? _auditTo = DateTime.Today;
    [ObservableProperty] private string _auditUser = "";
    [ObservableProperty] private string _auditAction = "";
    [ObservableProperty] private bool _canAudit = true;

    // SCRUM-129 · HU-18.1 (RF-105): período a exportar.
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
        var summary = await api.GetDashboardAsync(Grouping.Value);
        var audit = await SearchAuditAsync();

        // Se reemplaza todo junto, después de las llamadas, para que dos cargas seguidas no mezclen filas.
        ShowSummary(summary);
        Audit.Clear();
        foreach (var a in audit) Audit.Add(a);
    });

    private async Task LoadDashboardAsync() => ShowSummary(await api.GetDashboardAsync(Grouping.Value));

    private void ShowSummary(DashboardSummary summary)
    {
        Summary = summary;
        SalesByCashier.Clear();
        foreach (var row in summary.SalesByCashier.OrderByDescending(r => r.Day).ThenBy(r => r.CashierName))
            SalesByCashier.Add(row);
    }

    /// <summary>SCRUM-31 · HU-18 (RF-068 a RF-072, RF-103): muestra el reporte elegido.</summary>
    [RelayCommand]
    private Task ViewReportAsync() => RunAsync(async () =>
    {
        var (from, to) = ReportRange();
        Report = await api.GetReportAsync(ReportKind.Kind, from, to, Branch);
        ReportPreview = string.Join(Environment.NewLine, ReportPdf.Lines(Report, 140));
        if (Report.Notice.Length > 0) Info = Report.Notice;
    });

    /// <summary>SCRUM-31 · HU-18 (RF-105): el mismo reporte en Excel («xlsx») o PDF («pdf»).</summary>
    [RelayCommand]
    private Task ExportReportAsync(string format) => RunAsync(async () =>
    {
        var (from, to) = ReportRange();
        var file = await api.DownloadReportAsync(ReportKind.Kind, from, to, Branch, format);
        ExportedPath = await Downloads.SaveAsync(file.Content, file.FileName);
        Info = $"Reporte guardado en {ExportedPath}.";
    });

    /// <summary>SCRUM-34 · HU-29 (RF-100): resumen de una sucursal (requiere conexión).</summary>
    [RelayCommand]
    private Task BranchSummaryAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Branch))
            throw new Pharmacy.Domain.Common.DomainException("Escriba el nombre de la sucursal.");
        var (from, to) = ReportRange();
        var s = await api.GetBranchSummaryAsync(Branch.Trim(), from, to);
        BranchSummary = $"Sucursal {s.Branch} · puestos: {(s.Devices.Count == 0 ? "sin asignar" : string.Join(", ", s.Devices))}\n" +
                        $"{s.Sales} ventas por Bs {s.SalesTotal:N2} · {s.CashSessions} turnos de caja\n" +
                        string.Join("\n", s.StockMovements.Select(m => $"{m.ProductCode}  {m.ProductName}: {m.NetQuantity:+0;-0;0} u."));
    });

    private (DateOnly From, DateOnly To) ReportRange()
    {
        var to = DateOnly.FromDateTime(ReportTo ?? DateTime.Today);
        var from = IsRange ? DateOnly.FromDateTime(ReportFrom ?? DateTime.Today) : to;
        return (from, to);
    }

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

    /// <summary>SCRUM-129 · HU-18.1 (RF-105): guarda el .xlsx del período en la carpeta de descargas.</summary>
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
