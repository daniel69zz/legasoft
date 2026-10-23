using Microsoft.EntityFrameworkCore;
using Pharmacy.ServiceDefaults;

namespace Sync.Api;

/// <summary>Evento recibido de un puesto. Su eventId es la clave de idempotencia (RF-102).</summary>
public class ReceivedEvent
{
    public Guid EventId { get; set; }
    public string DeviceId { get; set; } = "";
    public string Module { get; set; } = "";
    public string EventType { get; set; } = "";
    public Guid EntityId { get; set; }
    public int Version { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string Payload { get; set; } = "";
}

/// <summary>Última versión aplicada por módulo y dispositivo (VersionManager).</summary>
public class ModuleVersion
{
    public string DeviceId { get; set; } = "";
    public string Module { get; set; } = "";
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SyncDb(DbContextOptions<SyncDb> options) : DbContext(options)
{
    public DbSet<ReceivedEvent> ReceivedEvents => Set<ReceivedEvent>();
    public DbSet<ModuleVersion> ModuleVersions => Set<ModuleVersion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b) =>
        b.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("sync");
        b.Entity<ReceivedEvent>(e =>
        {
            e.HasKey(x => x.EventId);
            e.HasIndex(x => new { x.DeviceId, x.Timestamp });
        });
        b.Entity<ModuleVersion>().HasKey(x => new { x.DeviceId, x.Module });
    }
}
