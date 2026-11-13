using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Pharmacy.Desktop.Converters;
using Pharmacy.Desktop.ViewModels;
using Pharmacy.Desktop.Views;

namespace Pharmacy.Desktop.UiTests;

/// <summary>
/// Pantalla Compras con la configuración regional de Bolivia (es-BO), donde la coma es el
/// separador decimal: los montos se escriben con punto o con coma y el total se recalcula al escribir.
/// </summary>
public class PurchasesInputTests
{
    [Theory]
    [InlineData("0.28", 0.28)]
    [InlineData("0,28", 0.28)]
    [InlineData("1.234,50", 1234.50)]
    [InlineData("1,234.50", 1234.50)]
    [InlineData("45", 45)]
    [Trait("JiraIssue", "SCRUM-124")]
    [Trait("Requirement", "RF-034")]
    public void Los_montos_aceptan_punto_o_coma_como_decimal(string texto, double esperado)
    {
        Assert.Equal((decimal)esperado, DecimalText.Parse(texto));
    }

    [AvaloniaFact]
    [Trait("JiraIssue", "SCRUM-19")]
    [Trait("JiraIssue", "SCRUM-124")]
    [Trait("Requirement", "RF-031")]
    [Trait("Requirement", "RF-034")]
    public void En_Compras_el_precio_y_el_costo_con_punto_y_el_total_se_recalcula_al_escribir()
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es-BO");
        var vm = new PurchasesViewModel(null!, null!, null!);
        var producto = new ProductOption(Guid.NewGuid(), "P-0001", "Paracetamol 500 mg");
        vm.ProductOptions.Add(producto);
        var window = new Window { Content = new PurchasesView { DataContext = vm }, Width = 1400, Height = 1000 };
        window.Show();

        // Precio ofrecido por el proveedor (tarjeta de precios, visible al elegir el producto).
        vm.ProductToAdd = producto;
        Settle();
        Escribir(window, Campo(window, n => n.DataContext == vm && n.TextConverter is not null), "0.28");
        Assert.Equal(0.28m, vm.NewPrice);

        // Línea de la compra: costo con punto y cantidad; el total se recalcula sin salir del campo.
        vm.AddProductCommand.Execute(null);
        Settle();
        var linea = vm.Draft.Single();
        Escribir(window, Campo(window, n => n.DataContext == linea && n.TextConverter is not null), "0.30");
        Escribir(window, Campo(window, n => n.DataContext == linea && n.TextConverter is null), "40");
        Assert.Equal((0.30m, 40m, 12.00m), (linea.UnitCost, linea.Quantity, vm.DraftTotal));
        var total = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text?.StartsWith("Total Bs") == true);
        Assert.Equal("Total Bs 12,00", total.Text);
    }

    private static NumericUpDown Campo(Window window, Func<NumericUpDown, bool> filtro) =>
        window.GetVisualDescendants().OfType<NumericUpDown>().Single(n => n.IsEffectivelyVisible && filtro(n));

    private static void Escribir(Window window, NumericUpDown campo, string texto)
    {
        var caja = campo.GetVisualDescendants().OfType<TextBox>().First();
        caja.Focus();
        caja.SelectAll();
        window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        window.KeyTextInput(texto);
        Settle();
    }

    private static void Settle()
    {
        for (var i = 0; i < 3; i++) Dispatcher.UIThread.RunJobs();
    }
}
