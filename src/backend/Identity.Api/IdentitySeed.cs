using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Domain.Identity;

namespace Identity.Api;

/// <summary>Un usuario de demostración por rol y dos puestos autorizados (tarea T-21 del Sprint 1).</summary>
public static class IdentitySeed
{
    public static readonly (string UserName, string FullName, string Role)[] DemoUsers =
    [
        ("admin", "Administrador del sistema", Roles.Administrador),
        ("gerente", "Gerente General", Roles.GerenteGeneral),
        ("contadora", "Contadora", Roles.Contadora),
        ("operaciones", "Gerente de Operaciones", Roles.GerenteOperaciones),
        ("cajero1", "Cajero turno mañana", Roles.Cajero),
        ("cajero2", "Cajero turno tarde", Roles.Cajero),
        ("farmaceutico", "Farmacéutico regente", Roles.Farmaceutico),
        ("almacen", "Encargado de almacén", Roles.Almacen),
    ];

    public static async Task RunAsync(IdentityDb db, string demoPassword)
    {
        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        foreach (var (userName, fullName, role) in DemoUsers)
        {
            var user = new User
            {
                Id = Guid.NewGuid(), UserName = userName, FullName = fullName, Role = role,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.PasswordHash = hasher.HashPassword(user, demoPassword);
            db.Users.Add(user);
        }
        db.Devices.AddRange(
            new AuthorizedDevice { DeviceId = "PUESTO-01", Description = "Caja principal" },
            new AuthorizedDevice { DeviceId = "PUESTO-02", Description = "Caja secundaria" });
        await db.SaveChangesAsync();
    }
}
