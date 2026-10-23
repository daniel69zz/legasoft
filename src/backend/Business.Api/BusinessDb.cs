using Microsoft.EntityFrameworkCore;
using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Inventory;
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
    }
}
