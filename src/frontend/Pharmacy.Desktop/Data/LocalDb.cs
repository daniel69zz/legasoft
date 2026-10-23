using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Sales;

namespace Pharmacy.Desktop.Data;

/// <summary>Credencial guardada como hash para iniciar sesión sin conexión (RNF-006).</summary>
public class LocalUser
{
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTimeOffset CachedAt { get; set; }
}

/// <summary>Estado de sincronización del puesto (SyncStateRepository): versiones por módulo.</summary>
public class SyncState
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>
/// Única base SQLite del puesto con tablas separadas por módulo (ADR-007). Una
/// llamada a SaveChanges es una transacción: venta, stock, caja y Outbox juntos.
/// </summary>
public class LocalDb(DbContextOptions<LocalDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<SyncState> SyncState => Set<SyncState>();
    public DbSet<LocalUser> Users => Set<LocalUser>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // SQLite no ordena ni compara DateTimeOffset en SQL: se guarda como entero.
        b.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        b.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("inventory_products");
            e.Property(p => p.Id).ValueGeneratedNever();
            e.HasIndex(p => p.Code).IsUnique();
        });
        b.Entity<Lot>(e =>
        {
            e.ToTable("inventory_lots");
            e.Property(l => l.Id).ValueGeneratedNever();
            e.HasIndex(l => l.ProductId);
        });
        b.Entity<StockMovement>(e =>
        {
            e.ToTable("inventory_movements");
            e.Property(m => m.Id).ValueGeneratedNever();
        });
        b.Entity<Sale>(e =>
        {
            e.ToTable("sales_sales");
            e.Property(s => s.Id).ValueGeneratedNever();
            e.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId);
            e.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });
        b.Entity<SaleLine>(e =>
        {
            e.ToTable("sales_lines");
            e.Property(l => l.Id).ValueGeneratedNever();
        });
        b.Entity<CashSession>(e =>
        {
            e.ToTable("cash_sessions");
            e.Property(s => s.Id).ValueGeneratedNever();
            e.HasMany(s => s.Movements).WithOne().HasForeignKey(m => m.CashSessionId);
            e.Navigation(s => s.Movements).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });
        b.Entity<CashMovement>(e =>
        {
            e.ToTable("cash_movements");
            e.Property(m => m.Id).ValueGeneratedNever();
        });
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("sync_outbox");
            e.HasKey(m => m.EventId);
            e.HasIndex(m => new { m.Status, m.Timestamp });
        });
        b.Entity<SyncState>(e =>
        {
            e.ToTable("sync_state");
            e.HasKey(s => s.Key);
        });
        b.Entity<LocalUser>(e =>
        {
            e.ToTable("identity_cached_users");
            e.HasKey(u => u.UserName);
        });
    }
}
