using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Demo;
using Pharmacy.Domain.Identity;

namespace Pharmacy.Desktop.Data;

public static class LocalSeeder
{
    /// <summary>
    /// Crea la base del puesto y carga el catálogo de demostración. También guarda
    /// las credenciales de los usuarios de demostración para probar el modo sin conexión
    /// desde el primer arranque; en producción solo se guardan tras un login en línea.
    /// </summary>
    public static void Run(LocalDb db, string demoPassword)
    {
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

        if (!db.Products.Any())
        {
            db.Products.AddRange(DemoCatalog.Products());
            db.Lots.AddRange(DemoCatalog.Lots(DateOnly.FromDateTime(DateTime.Now)));
        }
        if (!db.Users.Any())
        {
            var hasher = new PasswordHasher<LocalUser>();
            foreach (var (user, name, role) in new[]
                     {
                         ("admin", "Administrador del sistema", Roles.Administrador),
                         ("cajero1", "Cajero turno mañana", Roles.Cajero),
                         ("cajero2", "Cajero turno tarde", Roles.Cajero),
                         ("farmaceutico", "Farmacéutico regente", Roles.Farmaceutico),
                     })
            {
                var local = new LocalUser { UserName = user, FullName = name, Role = role, CachedAt = DateTimeOffset.Now };
                local.PasswordHash = hasher.HashPassword(local, demoPassword);
                db.Users.Add(local);
            }
        }
        db.SaveChanges();
    }
}
