namespace Pharmacy.Domain.Identity;

/// <summary>Funciones del sistema que se habilitan por rol (RF-002).</summary>
public enum Permission
{
    Ventas,
    Caja,
    Inventario,
    Catalogo,
    Reportes,
    Usuarios,
    Sincronizacion,

    /// <summary>Registro y modificación de proveedores (SCRUM-18 · HU-08).</summary>
    Proveedores,

    /// <summary>Registro de compras y su recepción (SCRUM-19 · HU-09).</summary>
    Compras,

    /// <summary>SCRUM-30 · HU-27 (RF-096): aplicar descuentos en la venta.</summary>
    Descuentos,

    /// <summary>SCRUM-28 · HU-24 (RF-084 a RF-087): registro y consulta de clientes.</summary>
    Clientes,

    /// <summary>SCRUM-36 · HU-23 (RF-082, RF-083): respaldo y recuperación de la información.</summary>
    Respaldos,
}

/// <summary>
/// Matriz rol → permisos. Es la fuente única que usan el menú del Desktop y las
/// políticas de autorización de los servicios (tarea T-12 del Sprint 1).
/// </summary>
public static class RolePermissions
{
    private static readonly Dictionary<string, Permission[]> Matrix = new()
    {
        [Roles.Administrador] = Enum.GetValues<Permission>(),
        [Roles.GerenteGeneral] = [Permission.Reportes, Permission.Inventario, Permission.Descuentos],
        [Roles.Contadora] = [Permission.Reportes, Permission.Caja],
        [Roles.GerenteOperaciones] = [Permission.Inventario, Permission.Catalogo, Permission.Reportes, Permission.Compras],
        [Roles.Cajero] = [Permission.Ventas, Permission.Caja, Permission.Sincronizacion, Permission.Clientes],
        [Roles.Farmaceutico] = [Permission.Ventas, Permission.Inventario, Permission.Catalogo, Permission.Descuentos,
            Permission.Clientes],
        [Roles.Almacen] = [Permission.Inventario],
    };

    public static IReadOnlyList<Permission> For(string role) =>
        Matrix.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool Has(string role, Permission permission) => For(role).Contains(permission);
}
