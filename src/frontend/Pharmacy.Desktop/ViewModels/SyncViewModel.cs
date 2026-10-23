using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Sync;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Estado de la cola Outbox del puesto y sincronización manual (RF-080, RF-081, RF-102).</summary>
public sealed partial class SyncViewModel(Operations operations, SyncWorker worker, SyncStatus status)
    : ViewModelBase, IActivatable
{
    public SyncStatus Status { get; } = status;
    public ObservableCollection<OutboxMessage> Messages { get; } = [];

    public Task ActivateAsync() => RunAsync(LoadAsync);

    private async Task LoadAsync()
    {
        var messages = await operations.QueryAsync(db =>
            db.Outbox.OrderByDescending(m => m.Timestamp).Take(50).ToListAsync());
        Messages.Clear();
        foreach (var m in messages) Messages.Add(m);
    }

    [RelayCommand]
    private Task SyncNowAsync() => RunAsync(async () =>
    {
        var ok = await worker.SyncOnceAsync();
        await LoadAsync();
        if (!ok) Error = Status.LastError;
    }, "Sincronización ejecutada.");
}
