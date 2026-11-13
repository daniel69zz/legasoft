using Microsoft.EntityFrameworkCore;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;
using Pharmacy.Domain.Purchasing;
using Pharmacy.ServiceDefaults;

namespace Business.Api;

public class SaleRecord
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string CashierName { get; set; } = "";
    public Guid CashSessionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal CashReceived { get; set; }
    public decimal Change { get; set; }
    public string PrescriptionNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerTaxId { get; set; } = "";
    public string Status { get; set; } = "Completada";
    public string VoidReason { get; set; } = "";
    public List<SaleLineRecord> Lines { get; set; } = [];
}

public class SaleLineRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CashSessionRecord
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = "";
    public string CashierName { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public string Status { get; set; } = "Abierta";
    public decimal? SalesTotal { get; set; }
    public decimal? ExpensesTotal { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? CountedAmount { get; set; }
    public decimal? Difference { get; set; }
    public string Justification { get; set; } = "";
    public DateTimeOffset? ClosedAt { get; set; }

    // HU-16.2 (RF-061, RF-062): entrega al cajero entrante.
    public string HandoverTo { get; set; } = "";
    public decimal? HandoverReceivedAmount { get; set; }
    public DateTimeOffset? HandoverConfirmedAt { get; set; }
}

public class CashMovementRecord
{
    public Guid Id { get; set; }
    public Guid CashSessionId { get; set; }
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public string Category { get; set; } = "";
    public string UserName { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Bitácora de operaciones críticas con usuario, dispositivo y fecha (RF-075).</summary>
public class AuditEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string UserName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
    public Guid? EventId { get; set; }
}

/// <summary>HU-28.1 (RF-099): parámetro operativo configurable por el administrador.</summary>
public class SettingEntry
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
}

/// <summary>
/// Lote creado en el servidor (recepción de una compra) pendiente de llegar a los puestos.
/// La secuencia es la versión que usa la descarga incremental del Sync Worker.
/// </summary>
public class LotFeedEntry
{
    public long Sequence { get; set; }
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public string LotNumber { get; set; } = "";
    public DateOnly ExpirationDate { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Un DbContext para el Business Service con un schema por dominio (ADR-005).</summary>
public class BusinessDb(DbContextOptions<BusinessDb> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SaleRecord> Sales => Set<SaleRecord>();
    public DbSet<CashSessionRecord> CashSessions => Set<CashSessionRecord>();
    public DbSet<CashMovementRecord> CashMovements => Set<CashMovementRecord>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<LotFeedEntry> LotFeed => Set<LotFeedEntry>();
    public DbSet<SupplierPrice> SupplierPrices => Set<SupplierPrice>();
    public DbSet<SettingEntry> Settings => Set<SettingEntry>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        b.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
        b.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.ToTable("products", "inventory");
            e.Property(p => p.Id).ValueGeneratedNever();
            e.HasIndex(p => p.Code).IsUnique();
            e.Ignore(p => p.DisplayName);
        });
        b.Entity<Lot>(e =>
        {
            e.ToTable("lots", "inventory");
            e.Property(l => l.Id).ValueGeneratedNever();
            e.HasIndex(l => l.ProductId);
        });
        b.Entity<StockMovement>(e =>
        {
            e.ToTable("stock_movements", "inventory");
            e.Property(m => m.Id).ValueGeneratedNever();
            e.Property(m => m.Type).HasConversion<string>();
        });
        b.Entity<SaleRecord>(e =>
        {
            e.ToTable("sales", "sales");
            e.Property(s => s.Id).ValueGeneratedNever();
            e.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId);
            e.HasIndex(s => s.CreatedAt);
        });
        b.Entity<SaleLineRecord>(e =>
        {
            e.ToTable("sale_lines", "sales");
            e.Property(l => l.Id).ValueGeneratedNever();
        });
        b.Entity<CashSessionRecord>(e =>
        {
            e.ToTable("sessions", "cash");
            e.Property(s => s.Id).ValueGeneratedNever();
        });
        b.Entity<CashMovementRecord>(e =>
        {
            e.ToTable("movements", "cash");
            e.Property(m => m.Id).ValueGeneratedNever();
        });
        b.Entity<AuditEntry>(e =>
        {
            e.ToTable("entries", "audit");
            e.HasIndex(a => a.OccurredAt);
        });

        // Proveedores y compras (HU-08, HU-09). Van en el schema inventory, que ya pertenece a
        // business_svc: un schema nuevo exigiría recrear el volumen de PostgreSQL (deploy/postgres).
        b.Entity<Supplier>(e =>
        {
            e.ToTable("suppliers", "inventory");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.TaxId).IsUnique();
        });
        b.Entity<Purchase>(e =>
        {
            e.ToTable("purchases", "inventory");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.SupplierId);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseId);
            e.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            e.Ignore(x => x.Total);
            e.Ignore(x => x.ReceivedTotal);
        });
        b.Entity<PurchaseLine>(e =>
        {
            e.ToTable("purchase_lines", "inventory");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.ProductId);
            e.Ignore(x => x.Subtotal);
            e.Ignore(x => x.MissingQuantity);
            e.Ignore(x => x.Difference);
        });
        b.Entity<SupplierPrice>(e =>
        {
            e.ToTable("supplier_prices", "inventory");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => new { x.SupplierId, x.ProductId });
            e.HasIndex(x => x.ProductId);
        });
        b.Entity<SettingEntry>(e =>
        {
            e.ToTable("settings", "inventory");
            e.HasKey(x => x.Key);
        });
        b.Entity<LotFeedEntry>(e =>
        {
            e.ToTable("lot_feed", "inventory");
            e.HasKey(x => x.Sequence);
            e.Property(x => x.Sequence).ValueGeneratedOnAdd();
        });
    }
}
