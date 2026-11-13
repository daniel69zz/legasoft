using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>Consultas incorporadas al Sprint 2: movimientos, auditoría filtrada, exportación, parámetros y precios.</summary>
public sealed partial class ApiClient
{
    public Task<List<InventoryMovementDto>> GetMovementsAsync(DateOnly from, DateOnly to, string? type) =>
        SendAsync<List<InventoryMovementDto>>(HttpMethod.Get,
            $"/api/inventory/movements?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" +
            (string.IsNullOrEmpty(type) ? "" : $"&type={Uri.EscapeDataString(type)}"));

    public Task<OperationalSettingsDto> GetSettingsAsync() =>
        SendAsync<OperationalSettingsDto>(HttpMethod.Get, "/api/settings");

    public Task<OperationalSettingsDto> SaveSettingsAsync(OperationalSettingsDto settings) =>
        SendAsync<OperationalSettingsDto>(HttpMethod.Put, "/api/settings", settings);

    public Task<SupplierPriceDto> RegisterSupplierPriceAsync(Guid supplierId, RegisterSupplierPriceRequest request) =>
        SendAsync<SupplierPriceDto>(HttpMethod.Post, $"/api/suppliers/{supplierId}/prices", request);

    public Task<List<SupplierPriceDto>> GetSupplierPricesAsync(Guid supplierId) =>
        SendAsync<List<SupplierPriceDto>>(HttpMethod.Get, $"/api/suppliers/{supplierId}/prices");

    public Task<List<SupplierPriceDto>> GetPriceHistoryAsync(Guid supplierId, Guid productId) =>
        SendAsync<List<SupplierPriceDto>>(HttpMethod.Get, $"/api/suppliers/{supplierId}/prices/history?productId={productId}");

    public Task<List<SupplierPriceDto>> ComparePricesAsync(Guid productId) =>
        SendAsync<List<SupplierPriceDto>>(HttpMethod.Get, $"/api/products/{productId}/supplier-prices");
}
