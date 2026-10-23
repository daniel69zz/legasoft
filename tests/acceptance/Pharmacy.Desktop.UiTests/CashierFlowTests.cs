using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Desktop.ViewModels;
using Pharmacy.Desktop.Views;

namespace Pharmacy.Desktop.UiTests;

/// <summary>
/// Recorre la jornada de un cajero en la interfaz real: inicio de sesión, apertura
/// de caja, venta con FEFO y cierre. Funciona con el backend levantado o sin él
/// (modo sin conexión). Guarda capturas en la carpeta SCREENSHOTS_DIR si se define.
/// </summary>
public class CashierFlowTests
{
    private static readonly string? Screenshots = Environment.GetEnvironmentVariable("SCREENSHOTS_DIR");

    [AvaloniaFact]
    public async Task Cajero_abre_caja_vende_y_cierra()
    {
        var main = App.Services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = main, Width = 1440, Height = 900 };
        window.Show();
        await Settle();
        Capture(window, "01-login");

        var login = Assert.IsType<LoginViewModel>(main.Current);
        login.UserName = "cajero1";
        login.Password = "Farmacia2026!";
        await login.LoginCommand.ExecuteAsync(null);
        var shell = Assert.IsType<ShellViewModel>(main.Current);
        Assert.DoesNotContain(shell.Items, i => i.Title == "Usuarios");

        // Caja
        shell.Selected = shell.Items.Single(i => i.Title == "Caja");
        var cash = Assert.IsType<CashViewModel>(shell.Page);
        await cash.ActivateAsync();
        if (!cash.IsOpen) await cash.OpenCommand.ExecuteAsync(null);
        Assert.True(cash.IsOpen, cash.Error);

        // Venta: 130 tabletas de paracetamol consumen primero el lote que vence antes (FEFO)
        shell.Selected = shell.Items.Single(i => i.Title == "Punto de venta");
        var pos = Assert.IsType<PosViewModel>(shell.Page);
        await pos.ActivateAsync();
        var paracetamol = pos.Results.Single(r => r.Code == "P-0001");
        pos.AddCommand.Execute(paracetamol);
        pos.Cart.Single().Quantity = 130;
        pos.AddCommand.Execute(pos.Results.Single(r => r.Code == "P-0004"));
        pos.CashReceived = 100m;
        await Settle();
        Capture(window, "02-carrito");
        await pos.ConfirmCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(pos.Error), pos.Error);
        Assert.Contains("PCM-2401", pos.Receipt);
        Assert.Contains("PCM-2409", pos.Receipt);
        await Settle();
        Capture(window, "03-venta-recibo");

        // Inventario con alertas
        shell.Selected = shell.Items.Single(i => i.Title == "Caja");
        cash = Assert.IsType<CashViewModel>(shell.Page);
        await cash.ActivateAsync();
        Assert.Equal(266.50m, cash.Expected);
        cash.Denominations.Single(d => d.Value == 200m).Count = 1;
        cash.Denominations.Single(d => d.Value == 50m).Count = 1;
        cash.Denominations.Single(d => d.Value == 10m).Count = 1;
        cash.Denominations.Single(d => d.Value == 5m).Count = 1;
        await Settle();
        Capture(window, "04-caja-conteo");
        await cash.CloseCommand.ExecuteAsync(null);
        Assert.Contains("RF-106", cash.Error);   // falta Bs 1,50 y no hay justificación
        cash.Justification = "Faltante de Bs 1,50 por cambio en monedas";
        await cash.CloseCommand.ExecuteAsync(null);
        Assert.False(cash.IsOpen, cash.Error);
        await Settle();
        Capture(window, "05-caja-cierre");

        shell.Selected = shell.Items.Single(i => i.Title == "Sincronización");
        var sync = Assert.IsType<SyncViewModel>(shell.Page);
        await sync.SyncNowCommand.ExecuteAsync(null);
        await sync.ActivateAsync();
        Assert.Equal(3, sync.Messages.Count);   // apertura, venta y cierre de caja
        await Settle();
        Capture(window, "06-sincronizacion");

        shell.LogoutCommand.Execute(null);
        login = Assert.IsType<LoginViewModel>(main.Current);
        login.UserName = "farmaceutico";
        login.Password = "Farmacia2026!";
        await login.LoginCommand.ExecuteAsync(null);
        shell = Assert.IsType<ShellViewModel>(main.Current);
        shell.Selected = shell.Items.Single(i => i.Title == "Inventario");
        var inventory = Assert.IsType<InventoryViewModel>(shell.Page);
        await inventory.ActivateAsync();
        Assert.Contains(inventory.ExpiryAlerts, a => a.LotNumber == "IBU-2312");
        await Settle();
        Capture(window, "07-inventario-alertas");

        // Con el backend en línea: el administrador ve usuarios y reportes consolidados.
        shell.LogoutCommand.Execute(null);
        login = Assert.IsType<LoginViewModel>(main.Current);
        login.UserName = "admin";
        login.Password = "Farmacia2026!";
        await login.LoginCommand.ExecuteAsync(null);
        shell = Assert.IsType<ShellViewModel>(main.Current);
        Assert.Contains(shell.Items, i => i.Title == "Usuarios");
        if (App.Services.GetRequiredService<Services.Session>().Token is null) return; // sin servidor

        shell.Selected = shell.Items.Single(i => i.Title == "Usuarios");
        var users = Assert.IsType<UsersViewModel>(shell.Page);
        await users.ActivateAsync();
        Assert.Contains(users.Users, u => u.UserName == "cajero1");
        await Settle();
        Capture(window, "08-usuarios");

        shell.Selected = shell.Items.Single(i => i.Title == "Reportes");
        var reports = Assert.IsType<ReportsViewModel>(shell.Page);
        await reports.ActivateAsync();
        Assert.NotNull(reports.Summary);
        await Settle();
        Capture(window, "09-reportes");
    }

    private static async Task Settle()
    {
        for (var i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(60);
        }
    }

    private static void Capture(Avalonia.Controls.Window window, string name)
    {
        if (Screenshots is null) return;
        Directory.CreateDirectory(Screenshots);
        window.CaptureRenderedFrame()?.Save(Path.Combine(Screenshots, $"{name}.png"));
    }
}
