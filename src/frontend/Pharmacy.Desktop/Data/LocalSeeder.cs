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
        // EnsureCreated no altera una base existente si el modelo incorpora tablas nuevas.
        db.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS sync_downloads (
                Id INTEGER NOT NULL CONSTRAINT PK_sync_downloads PRIMARY KEY AUTOINCREMENT,
                OccurredAt INTEGER NOT NULL,
                Module TEXT NOT NULL,
                ItemCount INTEGER NOT NULL,
                Summary TEXT NOT NULL,
                Details TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_sync_downloads_OccurredAt ON sync_downloads (OccurredAt);
            CREATE TABLE IF NOT EXISTS cash_arqueos (
                Id TEXT NOT NULL CONSTRAINT PK_cash_arqueos PRIMARY KEY,
                CashSessionId TEXT NOT NULL,
                ExpectedAmount REAL NOT NULL,
                CountedAmount REAL NOT NULL,
                Note TEXT NOT NULL,
                UserName TEXT NOT NULL,
                OccurredAt INTEGER NOT NULL,
                CONSTRAINT FK_cash_arqueos_cash_sessions_CashSessionId FOREIGN KEY (CashSessionId)
                    REFERENCES cash_sessions (Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_cash_arqueos_CashSessionId ON cash_arqueos (CashSessionId);
            """);
        // Sprint 3: columnas nuevas en bases de puestos creadas antes (SCRUM-29 · HU-25).
        AddColumnIfMissing(db, "sales_sales", "PrescriptionDoctor", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "sales_sales", "PrescriptionPatient", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(db, "sales_sales", "PrescriptionIssuedOn", "TEXT NULL");

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

    /// <summary>EnsureCreated no agrega columnas a una base existente; se agregan aquí si faltan.</summary>
    private static void AddColumnIfMissing(LocalDb db, string table, string column, string definition)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) connection.Open();
        using (var check = connection.CreateCommand())
        {
            check.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}'";
            if (Convert.ToInt32(check.ExecuteScalar()) > 0) return;
        }
        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        alter.ExecuteNonQuery();
    }
}
