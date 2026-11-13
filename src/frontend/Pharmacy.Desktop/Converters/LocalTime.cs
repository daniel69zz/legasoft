using System.Globalization;
using Avalonia.Data.Converters;

namespace Pharmacy.Desktop.Converters;

/// <summary>
/// Muestra en hora local del puesto las fechas que llegan del servidor. PostgreSQL las guarda en
/// UTC, así que formatearlas tal cual mostraría la hora UTC (4 horas más en La Paz).
/// El formato va en ConverterParameter, por ejemplo 'dd/MM/yyyy HH:mm'.
/// </summary>
public sealed class LocalTime : IValueConverter
{
    public static readonly LocalTime Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        DateTimeOffset d => d.ToLocalTime().ToString(parameter as string ?? "dd/MM/yyyy HH:mm", culture),
        _ => value,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
