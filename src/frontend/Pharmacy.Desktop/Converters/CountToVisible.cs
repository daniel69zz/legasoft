using System.Globalization;
using Avalonia.Data.Converters;

namespace Pharmacy.Desktop.Converters;

/// <summary>Muestra una sección solo cuando la colección tiene elementos.</summary>
public sealed class CountToVisible : IValueConverter
{
    public static readonly CountToVisible Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 0;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
