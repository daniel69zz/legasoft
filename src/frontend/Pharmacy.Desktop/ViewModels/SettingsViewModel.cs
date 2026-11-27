using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// SCRUM-130 · HU-28.1 (RF-099): parámetros operativos de inventario. Se guardan en el servidor; los puestos
/// los reciben con la siguiente sincronización. En el Sprint 3 suma el descuento máximo (SCRUM-30 · HU-27),
/// los datos de la empresa y la sucursal (SCRUM-33 · HU-28), la sucursal de cada puesto (SCRUM-34 · HU-29) y
/// los respaldos (SCRUM-36 · HU-23).
/// </summary>
public sealed partial class SettingsViewModel(ApiClient api, SyncWorker sync) : ViewModelBase, IActivatable
{
    [ObservableProperty] private decimal? _expiryThresholdDays;
    [ObservableProperty] private decimal? _alertIntervalMinutes;
    [ObservableProperty] private decimal? _maxDiscountPercent;

    // SCRUM-33 · HU-28 (RF-097, RF-098)
    [ObservableProperty] private string _companyName = "";
    [ObservableProperty] private string _companyTaxId = "";
    [ObservableProperty] private string _companyAddress = "";
    [ObservableProperty] private string _companyPhone = "";
    [ObservableProperty] private string _branchName = "";
    [ObservableProperty] private string _branchAddress = "";

    // SCRUM-34 · HU-29 (RF-100)
    [ObservableProperty] private string _deviceToAssign = "";
    [ObservableProperty] private string _deviceBranch = "";

    // SCRUM-36 · HU-23 (RF-082, RF-083)
    [ObservableProperty] private BackupDto? _selectedBackup;
    [ObservableProperty] private bool _confirmRestore;

    public ObservableCollection<BranchDeviceDto> BranchDevices { get; } = [];
    public ObservableCollection<BackupDto> Backups { get; } = [];

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var settings = await api.GetSettingsAsync();
        ExpiryThresholdDays = settings.ExpiryThresholdDays;
        AlertIntervalMinutes = settings.AlertIntervalMinutes;
        MaxDiscountPercent = settings.MaxDiscountPercent;

        var company = await api.GetCompanyAsync();
        (CompanyName, CompanyTaxId, CompanyAddress, CompanyPhone, BranchName, BranchAddress) = (company.CompanyName,
            company.CompanyTaxId, company.CompanyAddress, company.CompanyPhone, company.BranchName, company.BranchAddress);

        await LoadBranchesAsync();
        await LoadBackupsAsync();
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        await api.SaveSettingsAsync(new OperationalSettingsDto((int)(ExpiryThresholdDays ?? 0),
            (int)(AlertIntervalMinutes ?? 0), (int)(MaxDiscountPercent ?? 0)));
        sync.Wake();
    }, "Parámetros guardados. Los puestos aplican el nuevo umbral al sincronizar.");

    [RelayCommand]
    private Task SaveCompanyAsync() => RunAsync(async () =>
    {
        await api.SaveCompanyAsync(new CompanyProfileDto(CompanyName, CompanyTaxId, CompanyAddress, CompanyPhone,
            BranchName, BranchAddress));
        sync.Wake();
    }, "Datos de la empresa guardados. Los recibos los muestran después de la siguiente sincronización.");

    [RelayCommand]
    private Task AssignBranchAsync() => RunAsync(async () =>
    {
        await api.AssignBranchAsync(new BranchDeviceDto(DeviceToAssign, DeviceBranch));
        DeviceToAssign = DeviceBranch = "";
        await LoadBranchesAsync();
    }, "Puesto asignado a la sucursal. Sus nuevas operaciones quedan marcadas con ella.");

    [RelayCommand]
    private Task CreateBackupAsync() => RunAsync(async () =>
    {
        var backup = await api.CreateBackupAsync();
        await LoadBackupsAsync();
        Info = $"Respaldo {backup.Name} generado el {backup.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} ({backup.Rows} filas).";
    });

    [RelayCommand]
    private Task RestoreBackupAsync() => RunAsync(async () =>
    {
        if (SelectedBackup is not { } backup)
            throw new Pharmacy.Domain.Common.DomainException("Elija el respaldo a recuperar.");
        if (!ConfirmRestore)
            throw new Pharmacy.Domain.Common.DomainException(
                "Marque la confirmación: la recuperación reemplaza la información actual del servidor.");
        var safety = await api.RestoreBackupAsync(backup.Name);
        ConfirmRestore = false;
        await LoadBackupsAsync();
        sync.Wake();
        Info = $"Información recuperada desde {backup.Name}. El estado anterior quedó en {safety.Name}.";
    });

    private async Task LoadBranchesAsync()
    {
        var devices = await api.GetBranchDevicesAsync();
        BranchDevices.Clear();
        foreach (var d in devices) BranchDevices.Add(d);
    }

    private async Task LoadBackupsAsync()
    {
        try
        {
            var backups = await api.GetBackupsAsync();
            Backups.Clear();
            foreach (var b in backups) Backups.Add(b);
        }
        catch (ApiException e) when (e.Status == 403)
        {
            Backups.Clear(); // solo el administrador maneja respaldos
        }
    }
}
