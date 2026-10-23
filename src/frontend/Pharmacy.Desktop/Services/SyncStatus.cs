using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Pharmacy.Desktop.Services;

/// <summary>Estado de conexión y de la cola Outbox que muestra la barra superior.</summary>
public sealed partial class SyncStatus : ObservableObject
{
    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private int _pending;
    [ObservableProperty] private DateTimeOffset? _lastSync;
    [ObservableProperty] private string _lastError = "";

    public string Summary => IsOnline
        ? Pending == 0 ? "En línea · todo sincronizado" : $"En línea · {Pending} pendientes"
        : $"Sin conexión · {Pending} pendientes en el puesto";

    public void Update(bool? online = null, int? pending = null, DateTimeOffset? lastSync = null, string? error = null) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (online is not null) IsOnline = online.Value;
            if (pending is not null) Pending = pending.Value;
            if (lastSync is not null) LastSync = lastSync;
            if (error is not null) LastError = error;
            OnPropertyChanged(nameof(Summary));
        });
}
