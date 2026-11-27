using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Pharmacy.Desktop.Converters;

/// <summary>
/// Ícono de línea (24 × 24) para cada opción del menú lateral, elegido por su título. Así cada entrada
/// del menú lleva ícono y texto sin cambiar el modelo de navegación.
/// </summary>
public sealed class NavIcon : IValueConverter
{
    public static readonly NavIcon Instance = new();

    private static readonly Dictionary<string, Geometry> Icons = new()
    {
        // Carrito
        ["Punto de venta"] = Geometry.Parse("M3,4 H5.5 L8,15 H18 L20.5,7 H6.4 M8.5,19.5 A1.3,1.3 0 1 1 11.1,19.5 A1.3,1.3 0 1 1 8.5,19.5 M15.5,19.5 A1.3,1.3 0 1 1 18.1,19.5 A1.3,1.3 0 1 1 15.5,19.5"),
        // Billete
        ["Caja"] = Geometry.Parse("M3,7 H21 V17 H3 Z M12,9.6 A2.4,2.4 0 1 1 12,14.4 A2.4,2.4 0 1 1 12,9.6 M6.5,10 V14 M17.5,10 V14"),
        // Caja de almacén
        ["Inventario"] = Geometry.Parse("M4,7.5 L12,3.5 L20,7.5 V16.5 L12,20.5 L4,16.5 Z M4,7.5 L12,11.5 L20,7.5 M12,11.5 V20.5"),
        // Cápsula
        ["Catálogo"] = Geometry.Parse("M7.5,8.5 H16.5 A3.5,3.5 0 0 1 16.5,15.5 H7.5 A3.5,3.5 0 0 1 7.5,8.5 Z M12,8.5 V15.5"),
        // Entradas y salidas
        ["Movimientos"] = Geometry.Parse("M7,20 V4 M3.5,7.5 L7,4 L10.5,7.5 M17,4 V20 M13.5,16.5 L17,20 L20.5,16.5"),
        // Planilla de pedido
        ["Compras"] = Geometry.Parse("M9,3.5 H15 V6.5 H9 Z M7,5 H5.5 V20.5 H18.5 V5 H17 M8.5,11 H15.5 M8.5,15 H13.5"),
        // Camión de reparto
        ["Proveedores"] = Geometry.Parse("M3,6 H14 V16 H3 Z M14,9.5 H18 L21,12.5 V16 H14 M5.2,18 A1.6,1.6 0 1 1 8.4,18 A1.6,1.6 0 1 1 5.2,18 M15.6,18 A1.6,1.6 0 1 1 18.8,18 A1.6,1.6 0 1 1 15.6,18"),
        // Barras
        ["Reportes"] = Geometry.Parse("M4,20 H20 M7,16.5 V11 M12,16.5 V5.5 M17,16.5 V8.5"),
        // Personas
        ["Usuarios"] = Geometry.Parse("M9,5 A3,3 0 1 1 9,11 A3,3 0 1 1 9,5 M3.5,20 A5.5,5.5 0 0 1 14.5,20 M15.5,5.3 A3,3 0 0 1 15.5,10.7 M17.5,14.2 A5.5,5.5 0 0 1 20.5,19"),
        // Controles deslizantes
        ["Parámetros"] = Geometry.Parse("M4,7 H7 M11,7 H20 M7,7 A2,2 0 1 1 11,7 A2,2 0 1 1 7,7 M4,17 H13 M17,17 H20 M13,17 A2,2 0 1 1 17,17 A2,2 0 1 1 13,17"),
        // Flechas de sincronización
        ["Sincronización"] = Geometry.Parse("M4.5,11 A7.5,7.5 0 0 1 18,7 M18.5,3.5 V7.5 H14.5 M19.5,13 A7.5,7.5 0 0 1 6,17 M5.5,20.5 V16.5 H9.5"),
    };

    private static readonly Geometry Fallback = Geometry.Parse("M12,4 A8,8 0 1 1 12,20 A8,8 0 1 1 12,4");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string title && Icons.TryGetValue(title, out var icon) ? icon : Fallback;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Iniciales del usuario para el distintivo del menú (por ejemplo «Cajero turno mañana» → «CT»).</summary>
public sealed class Initials : IValueConverter
{
    public static readonly Initials Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var words = (value as string ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(w => char.ToUpper(w[0], culture)));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
