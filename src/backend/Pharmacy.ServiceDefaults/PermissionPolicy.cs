using Microsoft.AspNetCore.Builder;
using Pharmacy.Domain.Identity;

namespace Pharmacy.ServiceDefaults;

public static class PermissionPolicy
{
    /// <summary>Autoriza según la matriz rol → permisos compartida con el Desktop (RF-002).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, Permission permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(p => p.RequireAssertion(context =>
            context.User.FindFirst("role")?.Value is { } role && RolePermissions.Has(role, permission)));

    /// <summary>Autoriza si el rol tiene al menos uno de los permisos (por ejemplo, listar proveedores para comprar).</summary>
    public static TBuilder RequireAnyPermission<TBuilder>(this TBuilder builder, params Permission[] permissions)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(p => p.RequireAssertion(context =>
            context.User.FindFirst("role")?.Value is { } role && permissions.Any(x => RolePermissions.Has(role, x))));
}
