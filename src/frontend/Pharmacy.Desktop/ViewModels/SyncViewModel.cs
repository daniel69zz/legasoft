using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Sync;
using Pharmacy.Desktop.Data;
using Pharmacy.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Estado de la cola Outbox del puesto y sincronización manual (RF-080, RF-081, RF-102).</summary>
public sealed partial class SyncViewModel(Operations operations, SyncWorker worker, SyncStatus status)
    : ViewModelBase, IActivatable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDownload))]
    private SyncDownload? _selectedDownload;

    public SyncStatus Status { get; } = status;
    public ObservableCollection<OutboxMessage> Messages { get; } = [];
    public ObservableCollection<SyncDownload> Downloads { get; } = [];
    public bool HasSelectedDownload => SelectedDownload is not null;

    public Task ActivateAsync() => RunAsync(LoadAsync);

    private async Task LoadAsync()
    {
        var messages = await operations.QueryAsync(db =>
            db.Outbox.OrderByDescending(m => m.Timestamp).Take(50).ToListAsync());
        Messages.Clear();
        foreach (var m in messages) Messages.Add(m);
        var downloads = await operations.QueryAsync(db =>
            db.SyncDownloads.OrderByDescending(d => d.OccurredAt).ThenByDescending(d => d.Id).Take(100).ToListAsync());
        Downloads.Clear();
        foreach (var download in downloads) Downloads.Add(download);
        SelectedDownload = Downloads.FirstOrDefault();
    }

    [RelayCommand]
    private Task SyncNowAsync() => RunAsync(async () =>
    {
        var ok = await worker.SyncOnceAsync();
        await LoadAsync();
        if (!ok) Error = Status.LastError;
    }, "Sincronización ejecutada.");
}
