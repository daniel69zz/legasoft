using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>
/// SCRUM-130 · HU-28.1 (RF-099): parámetros operativos de inventario. Se guardan en el servidor; los puestos
/// reciben el umbral de vencimiento con la siguiente sincronización.
/// </summary>
public sealed partial class SettingsViewModel(ApiClient api, SyncWorker sync) : ViewModelBase, IActivatable
{
    [ObservableProperty] private decimal? _expiryThresholdDays;
    [ObservableProperty] private decimal? _alertIntervalMinutes;

    public Task ActivateAsync() => RunAsync(async () =>
    {
        var settings = await api.GetSettingsAsync();
        ExpiryThresholdDays = settings.ExpiryThresholdDays;
        AlertIntervalMinutes = settings.AlertIntervalMinutes;
    });

    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
        await api.SaveSettingsAsync(new OperationalSettingsDto((int)(ExpiryThresholdDays ?? 0),
            (int)(AlertIntervalMinutes ?? 0)));
        sync.Wake();
    }, "Parámetros guardados. Los puestos aplican el nuevo umbral al sincronizar.");
}
