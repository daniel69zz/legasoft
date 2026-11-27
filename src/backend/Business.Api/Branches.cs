using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;

namespace Business.Api;

/// <summary>
/// SCRUM-34 · HU-29 (RF-100): sucursal de cada puesto. La asignación se guarda en inventory.settings
/// (clave «branch.device.PUESTO-01»); un puesto sin asignar pertenece a la sucursal configurada en los
/// datos de la empresa (SCRUM-33 · HU-28). Ventas, movimientos de caja y de stock quedan marcados con ella.
/// </summary>
public static class Branches
{
    /// <summary>Columna sombra en inventory.stock_movements (la entidad de dominio no conoce sucursales).</summary>
    public const string Column = "Branch";

    private const string Prefix = "branch.device.";

    public static async Task<string> ForDeviceAsync(BusinessDb db, string deviceId, CancellationToken ct = default)
    {
        var assigned = await db.Settings.FindAsync([Prefix + deviceId], ct);
        if (!string.IsNullOrWhiteSpace(assigned?.Value)) return assigned.Value;
        return (await CompanyProfile.GetAsync(db)).BranchName;
    }

    public static async Task<List<BranchDeviceDto>> DevicesAsync(BusinessDb db) =>
        (await db.Settings.Where(s => s.Key.StartsWith(Prefix)).ToListAsync())
        .Select(s => new BranchDeviceDto(s.Key[Prefix.Length..], s.Value))
        .OrderBy(d => d.DeviceId).ToList();

    public static async Task AssignAsync(BusinessDb db, BranchDeviceDto request, string userName, DateTimeOffset now)
    {
        var key = Prefix + request.DeviceId.Trim().ToUpperInvariant();
        var entry = await db.Settings.FindAsync(key);
        if (entry is null) db.Settings.Add(entry = new SettingEntry { Key = key });
        entry.Value = request.Branch.Trim();
        entry.UpdatedAt = now;
        entry.UpdatedBy = userName;
    }
}
