using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Pharmacy.Desktop.Services;
using Pharmacy.Desktop.ViewModels;

namespace Pharmacy.Desktop.UiTests;

/// <summary>
/// SCRUM-39 · HU-T04 (RNF-006): continuidad operativa sin conexión. La operación local la recorre
/// <see cref="CashierFlowTests"/> sin backend (venta, caja, inventario y ajustes); aquí se comprueba el
/// tiempo de reanudación de la sincronización y que una función de la nube avise sin bloquear el resto.
/// </summary>
public class ContinuityTests
{
    [Fact]
    [Trait("JiraIssue", "SCRUM-39")]
    [Trait("Requirement", "RNF-006")]
    public void Tras_un_corte_la_sincronizacion_se_reintenta_como_maximo_cada_5_minutos()
    {
        var interval = TimeSpan.FromSeconds(15);
        var delay = interval;
        var waits = new List<TimeSpan>();
        for (var failure = 0; failure < 12; failure++)   // 12 intentos fallidos seguidos (corte largo)
        {
            delay = SyncWorker.NextDelay(delay, ok: false, interval);
            waits.Add(delay);
        }

        Assert.Equal(TimeSpan.FromSeconds(30), waits[0]);
        Assert.All(waits, w => Assert.True(w <= TimeSpan.FromMinutes(5)));
        Assert.Equal(TimeSpan.FromMinutes(5), waits[^1]);
        Assert.Equal(interval, SyncWorker.NextDelay(delay, ok: true, interval));   // al volver, ritmo normal
    }

    [AvaloniaFact]
    [Trait("JiraIssue", "SCRUM-39")]
    [Trait("Requirement", "RNF-006")]
    public async Task Una_funcion_que_requiere_conexion_avisa_y_no_bloquea_la_pantalla()
    {
        var offline = new Session(new DesktopOptions());   // sin token: el puesto trabaja sin conexión
        var api = new ApiClient(App.Services.GetRequiredService<IHttpClientFactory>(), offline);
        var users = new UsersViewModel(api);

        await users.ActivateAsync();

        Assert.Contains("conexión", users.Error);
        Assert.False(users.IsBusy);
    }
}
