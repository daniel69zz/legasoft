using CommunityToolkit.Mvvm.ComponentModel;
using Pharmacy.Application.Abstractions;

namespace Pharmacy.Desktop.Services;

/// <summary>Usuario conectado en el puesto. El token vive solo en memoria (riesgo del Sprint 1).</summary>
public sealed partial class Session(DesktopOptions options) : ObservableObject, ICurrentUser
{
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _role = "";
    [ObservableProperty] private bool _isSignedIn;

    public string DeviceId => options.DeviceId;
    public string? Token { get; private set; }

    // Se conserva en memoria durante la sesión para reconectar sin pedirla de nuevo
    // cuando vuelve Internet tras un inicio de sesión sin conexión.
    internal string? Password { get; private set; }

    public void SignIn(string userName, string fullName, string role, string? token, string password)
    {
        UserName = userName;
        FullName = fullName;
        Role = role;
        Token = token;
        Password = password;
        IsSignedIn = true;
    }

    public void SetToken(string? token) => Token = token;

    public void SignOut()
    {
        Token = null;
        Password = null;
        IsSignedIn = false;
        UserName = FullName = Role = "";
    }
}
