using System.Globalization;
using Avalonia.Data.Converters;

namespace Pharmacy.Desktop.Converters;

/// <summary>Texto según un valor lógico, para botones que muestran solo la acción posible.</summary>
public sealed class BoolText(string whenTrue, string whenFalse) : IValueConverter
{
    /// <summary>Activo → «Desactivar»; inactivo → «Activar».</summary>
    public static readonly BoolText ActivateLabel = new("Desactivar", "Activar");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? whenTrue : whenFalse;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
