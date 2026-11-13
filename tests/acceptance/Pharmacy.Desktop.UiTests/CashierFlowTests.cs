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
    [Trait("JiraIssue", "SCRUM-2")]
    [Trait("JiraIssue", "SCRUM-65")]
    [Trait("JiraIssue", "SCRUM-62")]
    [Trait("JiraIssue", "SCRUM-66")]
    [Trait("Requirement", "RF-001")]
    [Trait("Requirement", "RF-052")]
    [Trait("Requirement", "RF-037")]
    [Trait("Requirement", "RF-058")]
    [Trait("JiraIssue", "SCRUM-101")]
    [Trait("Requirement", "RF-016")]
    [Trait("JiraIssue", "SCRUM-126")]
    [Trait("Requirement", "RF-061")]
    [Trait("Requirement", "RF-062")]
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
        Assert.DoesNotContain(shell.Items, i => i.Title == "Catálogo");

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

        // HU-16.2: cajero1 entrega la caja a cajero2, que confirma el monto contado y abre su turno.
        shell.Selected = shell.Items.Single(i => i.Title == "Caja");
        cash = Assert.IsType<CashViewModel>(shell.Page);
        await cash.ActivateAsync();
        Assert.True(cash.CanHandover, cash.Error);
        cash.IncomingCashier = "cajero2";
        await cash.RequestHandoverCommand.ExecuteAsync(null);
        Assert.True(cash.IsHandoverPending, cash.Error);
        shell.LogoutCommand.Execute(null);
        login = Assert.IsType<LoginViewModel>(main.Current);
        login.UserName = "cajero2";
        login.Password = "Farmacia2026!";
        await login.LoginCommand.ExecuteAsync(null);
        shell = Assert.IsType<ShellViewModel>(main.Current);
        shell.Selected = shell.Items.Single(i => i.Title == "Caja");
        cash = Assert.IsType<CashViewModel>(shell.Page);
        await cash.ActivateAsync();
        Assert.True(cash.CanConfirmHandover, cash.HandoverStatus);
        await Settle();
        Capture(window, "06b-caja-entrega");
        cash.ReceivedAmount = 265m;
        await cash.ConfirmHandoverCommand.ExecuteAsync(null);
        Assert.True(cash.IsOpen, cash.Error);
        Assert.Equal(265m, cash.OpeningShown);

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

        // HU-06.3: ajuste manual con motivo sobre un lote, también sin conexión.
        inventory.Selected = inventory.Rows.Single(r => r.Code == "P-0005");
        await Settle();
        var stockBefore = inventory.Selected!.Stock;
        Assert.True(inventory.CanAdjust);
        Assert.NotNull(inventory.Lot);
        inventory.Quantity = -3;
        await inventory.AdjustCommand.ExecuteAsync(null);
        Assert.Contains("RF-016", inventory.Error);   // sin motivo no se registra
        inventory.Reason = "Producto dañado";
        await inventory.AdjustCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(inventory.Error), inventory.Error);
        Assert.Equal(stockBefore - 3, inventory.Rows.Single(r => r.Code == "P-0005").Stock);
        Assert.Contains(inventory.Movements, m => m.Quantity == -3 && m.Reason == "Producto dañado");
        await Settle();
        Capture(window, "07b-inventario-ajuste");

        // Con el backend en línea: el administrador ve usuarios y reportes consolidados.
        shell.LogoutCommand.Execute(null);
        login = Assert.IsType<LoginViewModel>(main.Current);
        login.UserName = "admin";
        login.Password = "Farmacia2026!";
        await login.LoginCommand.ExecuteAsync(null);
        shell = Assert.IsType<ShellViewModel>(main.Current);
        Assert.Contains(shell.Items, i => i.Title == "Usuarios");
        Assert.Contains(shell.Items, i => i.Title == "Compras");
        Assert.Contains(shell.Items, i => i.Title == "Proveedores");
        if (App.Services.GetRequiredService<Services.Session>().Token is null) return; // sin servidor

        // HU-08 y HU-09 (en línea): proveedores de demostración y compra pendiente de recibir.
        shell.Selected = shell.Items.Single(i => i.Title == "Proveedores");
        var suppliers = Assert.IsType<SuppliersViewModel>(shell.Page);
        await suppliers.ActivateAsync();
        Assert.NotEmpty(suppliers.Suppliers);
        suppliers.Selected = suppliers.Suppliers[0];
        await Settle();
        Capture(window, "08a-proveedores");

        // HU-06.4, HU-28.1 y HU-18.1 (en línea): movimientos, parámetros y exportación a Excel.
        shell.Selected = shell.Items.Single(i => i.Title == "Movimientos");
        var movements = Assert.IsType<MovementsViewModel>(shell.Page);
        await movements.ActivateAsync();
        Assert.True(string.IsNullOrEmpty(movements.Error), movements.Error);
        await Settle();
        Capture(window, "08c-movimientos");

        shell.Selected = shell.Items.Single(i => i.Title == "Parámetros");
        var settings = Assert.IsType<SettingsViewModel>(shell.Page);
        await settings.ActivateAsync();
        Assert.True(settings.ExpiryThresholdDays > 0, settings.Error);
        await Settle();
        Capture(window, "08d-parametros");

        shell.Selected = shell.Items.Single(i => i.Title == "Compras");
        var purchases = Assert.IsType<PurchasesViewModel>(shell.Page);
        await purchases.ActivateAsync();
        Assert.True(string.IsNullOrEmpty(purchases.Error), purchases.Error);
        purchases.Selected = purchases.Purchases.FirstOrDefault(p => p.Status == "Pendiente")
                             ?? purchases.Purchases.FirstOrDefault();
        await Settle();
        Capture(window, "08b-compras");

        // HU-10.1: precio del proveedor y costo sugerido; HU-09: compra, recepción con dañadas y lote nuevo en el puesto.
        purchases.Supplier = purchases.Suppliers.Single(s => s.TaxId == "2030405060");
        purchases.ProductToAdd = purchases.ProductOptions.Single(p => p.Code == "P-0006");
        await Settle();
        purchases.NewPrice = 3.10m;
        await purchases.RegisterPriceCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(purchases.Error), purchases.Error);
        Assert.Contains(purchases.Comparison, c => c.Price == 3.10m);
        purchases.AddProductCommand.Execute(null);
        var line = Assert.Single(purchases.Draft);
        Assert.Equal(3.10m, line.UnitCost);   // costo propuesto con el precio vigente
        line.Quantity = 40;
        Assert.Equal(124m, purchases.DraftTotal);
        await purchases.CreateCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(purchases.Error), purchases.Error);
        Assert.Equal("Pendiente", purchases.Selected!.Status);
        var reception = Assert.Single(purchases.Reception);
        var lotNumber = $"SRO-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        reception.LotNumber = lotNumber;
        reception.ExpirationDate = DateTime.Today.AddDays(500);
        reception.Received = 36;
        reception.Damaged = 4;
        Assert.Equal(4, reception.Difference);
        await purchases.ReceiveCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(purchases.Error), purchases.Error);
        Assert.Equal("Recibida", purchases.Selected!.Status);
        Assert.Equal((36, 4), (purchases.Selected.Lines[0].ReceivedQuantity, purchases.Selected.Lines[0].DamagedQuantity));
        await Settle();
        Capture(window, "08e-compra-recibida");

        // HU-28.1: el umbral configurado llega al puesto con la sincronización.
        shell.Selected = shell.Items.Single(i => i.Title == "Parámetros");
        settings = Assert.IsType<SettingsViewModel>(shell.Page);
        await settings.ActivateAsync();
        settings.ExpiryThresholdDays = 95;
        await settings.SaveCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(settings.Error), settings.Error);
        Assert.True(await App.Services.GetRequiredService<Services.SyncWorker>().SyncOnceAsync());

        shell.Selected = shell.Items.Single(i => i.Title == "Inventario");
        inventory = Assert.IsType<InventoryViewModel>(shell.Page);
        await inventory.ActivateAsync();
        Assert.Equal(95, inventory.ExpiryThresholdDays);
        Assert.Contains(inventory.ExpiryAlerts, a => a.LotNumber == "SRO-2403");   // vence en 90 días
        inventory.Selected = inventory.Rows.Single(r => r.Code == "P-0006");
        await Settle();
        Assert.Contains(inventory.LotDetails, l => l.LotNumber == lotNumber && l.Quantity == 36);
        await Settle();
        Capture(window, "08f-inventario-lote-recibido");

        settings.ExpiryThresholdDays = 60;   // deja el servidor de pruebas como estaba
        await settings.SaveCommand.ExecuteAsync(null);

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
        ReportsViewModel.ExportFolder = Path.Combine(Path.GetTempPath(), $"farmacia-export-{Guid.NewGuid():N}");
        await reports.ExportCommand.ExecuteAsync(null);
        Assert.True(File.Exists(reports.ExportedPath), reports.Error);
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
