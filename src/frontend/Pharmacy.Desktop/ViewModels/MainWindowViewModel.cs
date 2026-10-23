using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>Alterna entre la pantalla de inicio de sesión y el área de trabajo.</summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    [ObservableProperty] private ViewModelBase _current;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        _current = CreateLogin();
    }

    private LoginViewModel CreateLogin()
    {
        var login = _services.GetRequiredService<LoginViewModel>();
        login.LoggedIn += () =>
        {
            var shell = _services.GetRequiredService<ShellViewModel>();
            shell.LoggedOut += () => Current = CreateLogin();
            Current = shell;
        };
        return login;
    }
}
