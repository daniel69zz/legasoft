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
        [Roles.GerenteGeneral] = [Permission.Reportes, Permission.Inventario],
        [Roles.Contadora] = [Permission.Reportes, Permission.Caja],
        [Roles.GerenteOperaciones] = [Permission.Inventario, Permission.Catalogo, Permission.Reportes],
        [Roles.Cajero] = [Permission.Ventas, Permission.Caja, Permission.Sincronizacion],
        [Roles.Farmaceutico] = [Permission.Ventas, Permission.Inventario, Permission.Catalogo],
        [Roles.Almacen] = [Permission.Inventario],
    };

    public static IReadOnlyList<Permission> For(string role) =>
        Matrix.TryGetValue(role, out var permissions) ? permissions : [];

    public static bool Has(string role, Permission permission) => For(role).Contains(permission);
}
