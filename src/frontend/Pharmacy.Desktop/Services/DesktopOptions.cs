namespace Pharmacy.Desktop.Services;

public sealed class DesktopOptions
{
    public string GatewayUrl { get; set; } = "http://localhost:5100";

    /// <summary>Identificador del puesto autorizado. Se cambia con --Desktop:DeviceId=PUESTO-02.</summary>
    public string DeviceId { get; set; } = "PUESTO-01";

    public string DatabasePath { get; set; } = "";
    public int SyncIntervalSeconds { get; set; } = 15;
    public string DemoPassword { get; set; } = "";

    public string ResolveDatabasePath()
    {
        if (!string.IsNullOrWhiteSpace(DatabasePath)) return DatabasePath;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LegasoftFarmacia");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{DeviceId.ToLowerInvariant()}.db");
    }
}
