using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Identity;

namespace Business.Api;

public sealed class BackupOptions
{
    /// <summary>Carpeta de los respaldos; en Docker es el volumen /backups.</summary>
    public string Directory { get; set; } = Path.Combine(Path.GetTempPath(), "farmacia-backups");

    /// <summary>SCRUM-41 · HU-T06: días que se conservan los respaldos automáticos.</summary>
    public int KeepDays { get; set; } = 14;

    /// <summary>SCRUM-41 · HU-T06: el respaldo automático se genera si el último tiene más de estas horas.</summary>
    public int EveryHours { get; set; } = 24;

    public bool Scheduled { get; set; } = true;
}

/// <summary>Respaldo y recuperación de la información del negocio (SCRUM-36 · HU-23).</summary>
public interface IBackupStore
{
    Task<BackupDto> CreateAsync(string kind, string createdBy, CancellationToken ct = default);
    Task<IReadOnlyList<BackupDto>> ListAsync(CancellationToken ct = default);
    Task RestoreAsync(string name, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}

public static class BackupKinds
{
    public const string Manual = "Manual";
    public const string Automatic = "Automático";
    public const string BeforeRestore = "Antes de restaurar";
}

/// <summary>
/// SCRUM-36 · HU-23 (RF-082, RF-083): respaldo lógico de los schemas del Business Service (sales, inventory,
/// cash y audit) con COPY de PostgreSQL, en un .zip con un manifiesto. La recuperación vacía esas tablas y
/// las vuelve a cargar en una sola transacción, en orden de claves foráneas; si algo falla no cambia nada.
/// Los usuarios y equipos (schema identity) se respaldan con el respaldo completo del VPS (deploy/vps).
/// </summary>
public sealed class PostgresBackupStore(BusinessDb db, BackupOptions options, ILogger<PostgresBackupStore> logger)
    : IBackupStore
{
    private static readonly string[] Schemas = ["sales", "inventory", "cash", "audit"];
    private const string ManifestEntry = "manifest.json";

    private sealed record TableManifest(string Schema, string Table, long Rows);

    private sealed record Manifest(string Name, DateTimeOffset CreatedAt, string CreatedBy, string Kind,
        string Migration, IReadOnlyList<TableManifest> Tables);

    public async Task<BackupDto> CreateAsync(string kind, string createdBy, CancellationToken ct = default)
    {
        System.IO.Directory.CreateDirectory(options.Directory);
        var now = DateTimeOffset.UtcNow;
        var name = $"respaldo-{now:yyyyMMdd-HHmmss}-{Slug(kind)}.zip";
        var path = Path.Combine(options.Directory, name);
        var conn = await OpenAsync(ct);
        var tables = await TablesAsync(conn, ct);
        var manifest = new List<TableManifest>();

        await using (var file = File.Create(path))
        using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
        {
            foreach (var (schema, table) in tables)
            {
                using var reader = await conn.BeginTextExportAsync($"COPY \"{schema}\".\"{table}\" TO STDOUT", ct);
                var data = await reader.ReadToEndAsync(ct);
                await using (var entry = new StreamWriter(zip.CreateEntry($"{schema}.{table}.tsv").Open(), Encoding.UTF8))
                    await entry.WriteAsync(data);
                manifest.Add(new TableManifest(schema, table, data.Count(c => c == '\n')));
            }
            var info = new Manifest(name, now, createdBy, kind, await MigrationAsync(ct), manifest);
            await using var m = zip.CreateEntry(ManifestEntry).Open();
            await JsonSerializer.SerializeAsync(m, info, cancellationToken: ct);
        }
        logger.LogInformation("Respaldo {Name} generado ({Tables} tablas)", name, manifest.Count);
        return ToDto(new Manifest(name, now, createdBy, kind, "", manifest), new FileInfo(path).Length);
    }

    public Task<IReadOnlyList<BackupDto>> ListAsync(CancellationToken ct = default)
    {
        if (!System.IO.Directory.Exists(options.Directory)) return Task.FromResult<IReadOnlyList<BackupDto>>([]);
        var list = new List<BackupDto>();
        foreach (var file in new DirectoryInfo(options.Directory).GetFiles("respaldo-*.zip"))
        {
            try
            {
                list.Add(ToDto(ReadManifest(file.FullName), file.Length));
            }
            catch (Exception e) when (e is InvalidDataException or JsonException or IOException)
            {
                logger.LogWarning("Respaldo ilegible {File}: {Error}", file.Name, e.Message);
            }
        }
        return Task.FromResult<IReadOnlyList<BackupDto>>(list.OrderByDescending(b => b.CreatedAt).ToList());
    }

    public async Task RestoreAsync(string name, CancellationToken ct = default)
    {
        var path = PathOf(name);
        var manifest = ReadManifest(path);
        if (manifest.Migration != await MigrationAsync(ct))
            throw new DomainException(
                $"El respaldo es de otra versión de la base ({manifest.Migration}); no se puede recuperar sobre la actual.");

        var conn = await OpenAsync(ct);
        var tables = await TablesAsync(conn, ct);
        var order = await DependencyOrderAsync(conn, tables, ct);
        using var zip = ZipFile.OpenRead(path);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // SCRUM-13 · HU-20: la bitácora es inalterable; solo la recuperación desactiva su trigger, dentro de
        // la transacción, para reemplazarla por la del respaldo.
        await ExecAsync(conn, "ALTER TABLE audit.entries DISABLE TRIGGER USER", ct);
        await ExecAsync(conn, "TRUNCATE " + string.Join(", ", tables.Select(t => $"\"{t.Schema}\".\"{t.Table}\"")) +
            " RESTART IDENTITY CASCADE", ct);
        foreach (var (schema, table) in order)
        {
            var entry = zip.GetEntry($"{schema}.{table}.tsv");
            if (entry is null) continue;   // tabla nueva que el respaldo no tenía
            using var source = new StreamReader(entry.Open(), Encoding.UTF8);
            var data = await source.ReadToEndAsync(ct);
            await using var writer = await conn.BeginTextImportAsync($"COPY \"{schema}\".\"{table}\" FROM STDIN", ct);
            await writer.WriteAsync(data);
        }
        await ResetIdentitiesAsync(conn, ct);
        await ExecAsync(conn, "ALTER TABLE audit.entries ENABLE TRIGGER USER", ct);
        await tx.CommitAsync(ct);
        logger.LogWarning("Información recuperada desde el respaldo {Name}", name);
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        File.Delete(PathOf(name));
        return Task.CompletedTask;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(ct);
        return conn;
    }

    private async Task<string> MigrationAsync(CancellationToken ct) =>
        (await db.Database.GetAppliedMigrationsAsync(ct)).LastOrDefault() ?? "";

    private static async Task<List<(string Schema, string Table)>> TablesAsync(NpgsqlConnection conn,
        CancellationToken ct)
    {
        var tables = new List<(string, string)>();
        await using var cmd = new NpgsqlCommand("""
            SELECT table_schema, table_name FROM information_schema.tables
            WHERE table_schema = ANY(@schemas) AND table_type = 'BASE TABLE' AND table_name <> '__ef_migrations_history'
            ORDER BY table_schema, table_name
            """, conn);
        cmd.Parameters.AddWithValue("schemas", Schemas);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) tables.Add((reader.GetString(0), reader.GetString(1)));
        return tables;
    }

    /// <summary>Carga primero las tablas referenciadas por claves foráneas.</summary>
    private static async Task<List<(string Schema, string Table)>> DependencyOrderAsync(NpgsqlConnection conn,
        List<(string Schema, string Table)> tables, CancellationToken ct)
    {
        var dependsOn = tables.ToDictionary(t => t, _ => new HashSet<(string, string)>());
        await using (var cmd = new NpgsqlCommand("""
            SELECT cn.nspname, c.relname, rn.nspname, r.relname
            FROM pg_constraint k
            JOIN pg_class c ON c.oid = k.conrelid JOIN pg_namespace cn ON cn.oid = c.relnamespace
            JOIN pg_class r ON r.oid = k.confrelid JOIN pg_namespace rn ON rn.oid = r.relnamespace
            WHERE k.contype = 'f'
            """, conn))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var child = (reader.GetString(0), reader.GetString(1));
                var parent = (reader.GetString(2), reader.GetString(3));
                if (dependsOn.ContainsKey(child) && dependsOn.ContainsKey(parent) && child != parent)
                    dependsOn[child].Add(parent);
            }
        }
        var ordered = new List<(string, string)>();
        var visited = new HashSet<(string, string)>();
        void Visit((string, string) t)
        {
            if (!visited.Add(t)) return;
            foreach (var parent in dependsOn[t]) Visit(parent);
            ordered.Add(t);
        }
        foreach (var t in tables) Visit(t);
        return ordered;
    }

    private static async Task ResetIdentitiesAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var columns = new List<(string, string, string)>();
        await using (var cmd = new NpgsqlCommand("""
            SELECT table_schema, table_name, column_name FROM information_schema.columns
            WHERE table_schema = ANY(@schemas) AND (is_identity = 'YES' OR column_default LIKE 'nextval%')
            """, conn))
        {
            cmd.Parameters.AddWithValue("schemas", Schemas);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct)) columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        foreach (var (schema, table, column) in columns)
            await ExecAsync(conn, $"""
                SELECT setval(pg_get_serial_sequence('"{schema}"."{table}"', '{column}'),
                              COALESCE((SELECT MAX("{column}") FROM "{schema}"."{table}"), 0) + 1, false)
                """, ct);
    }

    private static async Task ExecAsync(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private string PathOf(string name)
    {
        if (name.Contains('/') || name.Contains('\\') || !name.StartsWith("respaldo-") || !name.EndsWith(".zip"))
            throw new DomainException("Nombre de respaldo no válido.");
        var path = Path.Combine(options.Directory, name);
        return File.Exists(path) ? path : throw new DomainException($"No existe el respaldo {name}.");
    }

    private static Manifest ReadManifest(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        using var stream = (zip.GetEntry(ManifestEntry) ?? throw new InvalidDataException("Sin manifiesto")).Open();
        return JsonSerializer.Deserialize<Manifest>(stream) ?? throw new InvalidDataException("Manifiesto vacío");
    }

    private static BackupDto ToDto(Manifest m, long size) =>
        new(m.Name, m.CreatedAt, m.CreatedBy, m.Kind, size, m.Tables.Count, m.Tables.Sum(t => t.Rows));

    private static string Slug(string kind) => kind switch
    {
        BackupKinds.Automatic => "automatico",
        BackupKinds.BeforeRestore => "antes-de-restaurar",
        _ => "manual",
    };
}

/// <summary>
/// Respaldo para el proveedor InMemory (pruebas): guarda solo el registro, sin datos. Permite probar
/// permisos, auditoría y avisos sin PostgreSQL.
/// </summary>
public sealed class InMemoryBackupStore : IBackupStore
{
    private readonly List<BackupDto> _backups = [];
    public List<string> Restored { get; } = [];

    public Task<BackupDto> CreateAsync(string kind, string createdBy, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var backup = new BackupDto($"respaldo-{now:yyyyMMdd-HHmmss}-{_backups.Count}.zip", now, createdBy, kind, 0, 0, 0);
        lock (_backups) _backups.Add(backup);
        return Task.FromResult(backup);
    }

    public Task<IReadOnlyList<BackupDto>> ListAsync(CancellationToken ct = default)
    {
        lock (_backups) return Task.FromResult<IReadOnlyList<BackupDto>>(_backups.OrderByDescending(b => b.CreatedAt).ToList());
    }

    public Task RestoreAsync(string name, CancellationToken ct = default)
    {
        lock (_backups)
            if (_backups.All(b => b.Name != name)) throw new DomainException($"No existe el respaldo {name}.");
        Restored.Add(name);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string name, CancellationToken ct = default)
    {
        lock (_backups) _backups.RemoveAll(b => b.Name == name);
        return Task.CompletedTask;
    }
}

/// <summary>
/// SCRUM-41 · HU-T06 (RNF-008): estado del respaldo diario. El último resultado se guarda en
/// inventory.settings y, si falló o pasó más de un día sin respaldo, el Notification Worker lo avisa por correo.
/// </summary>
public static class BackupHealth
{
    public const string LastSuccessKey = "backup.last_success";
    public const string LastFailureKey = "backup.last_failure";

    public static async Task RecordAsync(BusinessDb db, bool ok, string detail, DateTimeOffset now)
    {
        var key = ok ? LastSuccessKey : LastFailureKey;
        var entry = await db.Settings.FindAsync(key);
        if (entry is null) db.Settings.Add(entry = new SettingEntry { Key = key });
        entry.Value = $"{now:O}|{detail}";
        entry.UpdatedAt = now;
        entry.UpdatedBy = "respaldo diario";
        if (ok && await db.Settings.FindAsync(LastFailureKey) is { } failure) db.Settings.Remove(failure);
        await db.SaveChangesAsync();
    }

    public static async Task<IReadOnlyList<AlertDto>> AlertsAsync(BusinessDb db, TimeProvider time)
    {
        var alerts = new List<AlertDto>();
        var failure = await db.Settings.FindAsync(LastFailureKey);
        if (failure is not null)
            alerts.Add(new AlertDto("Respaldo fallido", "Base central", Detail(failure.Value)));
        var success = await db.Settings.FindAsync(LastSuccessKey);
        if (success is not null && When(success.Value) is { } last && time.GetUtcNow() - last > TimeSpan.FromHours(26))
            alerts.Add(new AlertDto("Respaldo atrasado", "Base central",
                $"El último respaldo es del {last.ToOffset(BusinessEndpoints.LaPaz):dd/MM/yyyy HH:mm}."));
        return alerts;
    }

    public static DateTimeOffset? When(string value) =>
        DateTimeOffset.TryParse(value.Split('|')[0], out var at) ? at : null;

    private static string Detail(string value) => value.Contains('|') ? value[(value.IndexOf('|') + 1)..] : value;
}

/// <summary>
/// SCRUM-41 · HU-T06 (RNF-008): genera un respaldo automático si el último tiene más de 24 h (RPO de 24 h),
/// borra los automáticos más antiguos que el período de retención y registra el resultado.
/// </summary>
public sealed class BackupScheduler(IServiceScopeFactory scopes, BackupOptions options, TimeProvider time,
    ILogger<BackupScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ContinueWith(_ => { }, CancellationToken.None);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusinessDb>();
        var store = scope.ServiceProvider.GetRequiredService<IBackupStore>();
        try
        {
            var backups = await store.ListAsync(ct);
            var lastAuto = backups.Where(b => b.Kind == BackupKinds.Automatic).Select(b => (DateTimeOffset?)b.CreatedAt)
                .Max();
            if (lastAuto is null || time.GetUtcNow() - lastAuto > TimeSpan.FromHours(options.EveryHours))
            {
                var backup = await store.CreateAsync(BackupKinds.Automatic, "respaldo diario", ct);
                await BackupHealth.RecordAsync(db, true, backup.Name, time.GetUtcNow());
            }
            foreach (var old in backups.Where(b => b.Kind == BackupKinds.Automatic
                         && time.GetUtcNow() - b.CreatedAt > TimeSpan.FromDays(options.KeepDays)))
                await store.DeleteAsync(old.Name, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "El respaldo diario falló");
            db.ChangeTracker.Clear();
            await BackupHealth.RecordAsync(db, false, $"El respaldo diario falló: {e.Message}", time.GetUtcNow());
        }
    }
}

public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this WebApplication app)
    {
        var backups = app.MapGroup("/api/backups").WithTags("Respaldos")
            .RequireAuthorization(p => p.RequireAssertion(c =>
                c.User.FindFirst("role")?.Value is { } role && RolePermissions.Has(role, Permission.Respaldos)));

        backups.MapGet("", async (IBackupStore store) => await store.ListAsync())
            .WithSummary("[SCRUM-36 · HU-23] Respaldos disponibles con su fecha y hora (RF-082)")
            .Produces<IReadOnlyList<BackupDto>>();

        backups.MapPost("", async (IBackupStore store, BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                var backup = await store.CreateAsync(BackupKinds.Manual, Sprint3Endpoints.UserName(http));
                Sprint3Endpoints.Audit(db, http, time, "Respaldo generado",
                    $"{backup.Name} · {backup.Tables} tablas · {backup.Rows} filas");
                await db.SaveChangesAsync();
                return Results.Created($"/api/backups/{backup.Name}", backup);
            })
            .WithSummary("[SCRUM-36 · HU-23] Genera un respaldo y registra su fecha y hora (RF-082)")
            .Produces<BackupDto>(StatusCodes.Status201Created);

        backups.MapPost("/{name}/restore", async (string name, RestoreRequest request, IBackupStore store,
                BusinessDb db, HttpContext http, TimeProvider time) =>
            {
                if (!request.Confirm) throw new DomainException("Confirme la recuperación: reemplaza la información actual.");
                // Antes de recuperar se guarda el estado actual, por si hubo un error al elegir el respaldo.
                var safety = await store.CreateAsync(BackupKinds.BeforeRestore, Sprint3Endpoints.UserName(http));
                await store.RestoreAsync(name);
                db.ChangeTracker.Clear();
                Sprint3Endpoints.Audit(db, http, time, "Recuperación de respaldo",
                    $"Restaurado {name}; estado anterior guardado en {safety.Name}");
                await db.SaveChangesAsync();
                return Results.Ok(safety);
            })
            .WithSummary("[SCRUM-36 · HU-23] Recupera la información desde un respaldo; antes guarda el estado actual (RF-083)")
            .Produces<BackupDto>().ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
