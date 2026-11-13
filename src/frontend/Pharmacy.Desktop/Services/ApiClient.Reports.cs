using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>Consultas incorporadas al Sprint 2: movimientos, auditoría filtrada, exportación, parámetros y precios.</summary>
public sealed partial class ApiClient
{
    public Task<OperationalSettingsDto> GetSettingsAsync() =>
        SendAsync<OperationalSettingsDto>(HttpMethod.Get, "/api/settings");

    public Task<OperationalSettingsDto> SaveSettingsAsync(OperationalSettingsDto settings) =>
        SendAsync<OperationalSettingsDto>(HttpMethod.Put, "/api/settings", settings);
}
