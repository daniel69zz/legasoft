using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Application.Contracts;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Identity;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>HU-02: gestión de usuarios y roles por el Administrador (RF-002 a RF-005) con su auditoría.</summary>
public sealed partial class UsersViewModel(ApiClient api) : ViewModelBase, IActivatable
{
    [ObservableProperty] private string _newUserName = "";
    [ObservableProperty] private string _newFullName = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _newRole = Roles.Cajero;

    public IReadOnlyList<string> AvailableRoles => Roles.All;
    public ObservableCollection<UserDto> Users { get; } = [];
    public ObservableCollection<AuditEntryDto> Audit { get; } = [];

    public Task ActivateAsync() => RunAsync(LoadAsync);

    private async Task LoadAsync()
    {
        var users = await api.GetUsersAsync();
        var audit = await api.GetUserAuditAsync();
        Users.Clear();
        foreach (var u in users) Users.Add(u);
        Audit.Clear();
        foreach (var a in audit) Audit.Add(a);
    }

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        await api.CreateUserAsync(new CreateUserRequest(NewUserName, NewFullName, NewPassword, NewRole));
        NewUserName = NewFullName = NewPassword = "";
        await LoadAsync();
    }, "Usuario registrado.");

    [RelayCommand]
    private Task ToggleAsync(UserDto user) => RunAsync(async () =>
    {
        await api.SetUserActiveAsync(user.Id, !user.IsActive);
        await LoadAsync();
    }, user.IsActive ? $"{user.UserName} desactivado." : $"{user.UserName} activado.");
}
