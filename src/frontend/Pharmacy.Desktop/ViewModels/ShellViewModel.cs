using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Desktop.Services;
using Pharmacy.Domain.Identity;

namespace Pharmacy.Desktop.ViewModels;

public sealed record NavItem(string Title, string Subtitle, Permission Permission, Type ViewModel);

/// <summary>Área de trabajo con el menú dinámico según el rol (tarea T-17, RF-002).</summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    private static readonly NavItem[] AllItems =
    [
        new("Punto de venta", "Ventas en efectivo", Permission.Ventas, typeof(PosViewModel)),
        new("Caja", "Apertura, egresos y cierre", Permission.Caja, typeof(CashViewModel)),
        new("Inventario", "Stock, lotes y alertas", Permission.Inventario, typeof(InventoryViewModel)),
        new("Catálogo", "Registro de medicamentos", Permission.Catalogo, typeof(CatalogViewModel)),
        new("Reportes", "Dashboard gerencial", Permission.Reportes, typeof(ReportsViewModel)),
        new("Usuarios", "Usuarios y auditoría", Permission.Usuarios, typeof(UsersViewModel)),
        new("Sincronización", "Outbox del puesto", Permission.Sincronizacion, typeof(SyncViewModel)),
    ];

    private readonly IServiceProvider _services;

    [ObservableProperty] private NavItem? _selected;
    [ObservableProperty] private ViewModelBase? _page;

    public ShellViewModel(IServiceProvider services, Session session, SyncStatus status)
    {
        _services = services;
        Session = session;
        Status = status;
        Items = AllItems.Where(i => RolePermissions.Has(session.Role, i.Permission)).ToList();
        Selected = Items.FirstOrDefault();
    }

    public Session Session { get; }
    public SyncStatus Status { get; }
    public IReadOnlyList<NavItem> Items { get; }
    public string RoleLabel => Session.Role switch
    {
        Roles.GerenteGeneral => "Gerente General",
        Roles.GerenteOperaciones => "Gerente de Operaciones",
        Roles.Farmaceutico => "Farmacéutico",
        Roles.Almacen => "Almacén",
        var r => r,
    };

    public event Action? LoggedOut;

    partial void OnSelectedChanged(NavItem? value)
    {
        Page = value is null ? null : (ViewModelBase)_services.GetRequiredService(value.ViewModel);
        if (Page is IActivatable activatable) _ = activatable.ActivateAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        Session.SignOut();
        LoggedOut?.Invoke();
    }
}

/// <summary>Página que carga sus datos al mostrarse.</summary>
public interface IActivatable
{
    Task ActivateAsync();
}
