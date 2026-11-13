using System.Globalization;
using Avalonia.Data.Converters;

namespace Pharmacy.Desktop.Converters;

/// <summary>
/// Texto ↔ monto para los NumericUpDown de dinero. En es-BO la coma es el separador decimal y el
/// punto agrupa miles, así que «0.28» se leía como 28. Acepta punto o coma como separador decimal
/// (si aparecen los dos, el último es el decimal) y muestra el monto con el formato del puesto.
/// </summary>
public sealed class DecimalText(string format) : IValueConverter
{
    public static readonly DecimalText Money = new("N2");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Map(value, culture);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Map(value, culture);

    // NumericUpDown usa el convertidor en ambos sentidos; se resuelve según el tipo recibido.
    private object? Map(object? value, CultureInfo culture) => value switch
    {
        decimal d => d.ToString(format, culture),
        string s => Parse(s),
        null => null,
        _ => value,
    };

    public static decimal? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = new string(text.Trim().Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
        var last = t.LastIndexOfAny(['.', ',']);
        if (last >= 0)
            t = t[..last].Replace(".", "").Replace(",", "") + "." + t[(last + 1)..];
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
