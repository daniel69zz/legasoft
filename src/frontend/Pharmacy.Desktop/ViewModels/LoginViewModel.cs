using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pharmacy.Desktop.Services;

namespace Pharmacy.Desktop.ViewModels;

/// <summary>HU-01: inicio de sesión con credenciales (RF-001).</summary>
public sealed partial class LoginViewModel(AuthService auth, DesktopOptions options) : ViewModelBase
{
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _password = "";

    public event Action? LoggedIn;

    public string DeviceId => options.DeviceId;
    public string Server => options.GatewayUrl;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
        {
            Error = "Ingrese su usuario y contraseña.";
            return;
        }
        await RunAsync(async () =>
        {
            var error = await auth.LoginAsync(UserName, Password);
            if (error is not null)
            {
                Error = error;
                return;
            }
            Password = "";
            LoggedIn?.Invoke();
        });
    }
}
