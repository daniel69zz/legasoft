using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Business.Api;

/// <summary>
/// SCRUM-130 · HU-28.1 (RF-099): parámetros operativos guardados en inventory.settings. Si un parámetro no
/// se configuró, rige el valor por defecto del Sprint 1.
/// </summary>
public static class OperationalSettings
{
    public const string ExpiryThresholdDays = "ExpiryThresholdDays";
    public const string AlertIntervalMinutes = "AlertIntervalMinutes";

    public const int DefaultAlertIntervalMinutes = 60;

    public static async Task<OperationalSettingsDto> GetAsync(BusinessDb db)
    {
        var values = await db.Settings.ToDictionaryAsync(s => s.Key, s => s.Value);
        return new OperationalSettingsDto(
            Read(values, ExpiryThresholdDays, InventoryAlerts.DefaultExpiryThresholdDays),
            Read(values, AlertIntervalMinutes, DefaultAlertIntervalMinutes));
    }

    /// <summary>
    /// Para el Notification Worker: la frecuencia solo cuando el administrador la configuró (0 si no),
    /// así rige la del despliegue (ALERT_INTERVAL_MINUTES) hasta que alguien la cambie.
    /// </summary>
    public static async Task<OperationalSettingsDto> GetForWorkerAsync(BusinessDb db)
    {
        var settings = await GetAsync(db);
        var configured = await db.Settings.AnyAsync(s => s.Key == AlertIntervalMinutes);
        return settings with { AlertIntervalMinutes = configured ? settings.AlertIntervalMinutes : 0 };
    }

    public static void Validate(OperationalSettingsDto s)
    {
        if (s.ExpiryThresholdDays is < 1 or > 365)
            throw new DomainException("El umbral de vencimiento debe estar entre 1 y 365 días.");
        if (s.AlertIntervalMinutes is < 5 or > 1440)
            throw new DomainException("La frecuencia de las alertas debe estar entre 5 y 1440 minutos.");
    }

    public static async Task SaveAsync(BusinessDb db, OperationalSettingsDto s, string userName, DateTimeOffset now)
    {
        await SetAsync(db, ExpiryThresholdDays, s.ExpiryThresholdDays, userName, now);
        await SetAsync(db, AlertIntervalMinutes, s.AlertIntervalMinutes, userName, now);
    }

    private static async Task SetAsync(BusinessDb db, string key, int value, string userName, DateTimeOffset now)
    {
        var entry = await db.Settings.FindAsync(key);
        if (entry is null) db.Settings.Add(entry = new SettingEntry { Key = key });
        entry.Value = value.ToString();
        entry.UpdatedAt = now;
        entry.UpdatedBy = userName;
    }

    private static int Read(Dictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out var raw) && int.TryParse(raw, out var value) ? value : fallback;
}
