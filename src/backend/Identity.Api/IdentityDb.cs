using Microsoft.EntityFrameworkCore;

namespace Identity.Api;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTimeOffset? FirstFailedAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Computadora autorizada para que inicien sesión los cajeros (RF-006) y, si se indica, el administrador (RF-007).</summary>
public class AuthorizedDevice
{
    public string DeviceId { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;

    /// <summary>HU-03.2 (RF-007): el perfil Administrador solo inicia sesión en equipos con esta marca.</summary>
    public bool AllowsAdministrator { get; set; }
}

/// <summary>Asistencia registrada al iniciar sesión en un equipo autorizado (RF-008).</summary>
public class Attendance
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public DateOnly Day { get; set; }
    public DateTimeOffset FirstLoginAt { get; set; }
}

/// <summary>Bitácora de altas, cambios y activaciones de usuarios (RF-075).</summary>
public class UserChange
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string ChangedBy { get; set; } = "";
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
}

public class IdentityDb(DbContextOptions<IdentityDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AuthorizedDevice> Devices => Set<AuthorizedDevice>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<UserChange> UserChanges => Set<UserChange>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("identity");
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.UserName).IsUnique();
            e.Property(u => u.UserName).HasMaxLength(60);
        });
        b.Entity<AuthorizedDevice>(e =>
        {
            e.HasKey(d => d.DeviceId);
            // Los equipos registrados antes del Sprint 2 siguen admitiendo al administrador.
            e.Property(d => d.AllowsAdministrator).HasDefaultValue(true).ValueGeneratedNever();
        });
        b.Entity<Attendance>().HasIndex(a => new { a.UserName, a.Day, a.DeviceId }).IsUnique();
    }
}
