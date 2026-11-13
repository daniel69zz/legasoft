using System.Net.Http.Headers;
using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

public sealed record ExportFile(byte[] Content, string FileName, bool IsEmpty);

/// <summary>Consultas incorporadas al Sprint 2: movimientos, auditoría filtrada, exportación, parámetros y precios.</summary>
public sealed partial class ApiClient
{
    public Task<List<InventoryMovementDto>> GetMovementsAsync(DateOnly from, DateOnly to, string? type) =>
        SendAsync<List<InventoryMovementDto>>(HttpMethod.Get,
            $"/api/inventory/movements?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" +
            (string.IsNullOrEmpty(type) ? "" : $"&type={Uri.EscapeDataString(type)}"));

    public Task<List<AuditEntryDto>> SearchAuditAsync(DateOnly? from, DateOnly? to, string user, string action) =>
        SendAsync<List<AuditEntryDto>>(HttpMethod.Get,
            "/api/audit?" + string.Join("&", new[]
            {
                from is { } f ? $"from={f:yyyy-MM-dd}" : null,
                to is { } t ? $"to={t:yyyy-MM-dd}" : null,
                string.IsNullOrWhiteSpace(user) ? null : $"user={Uri.EscapeDataString(user)}",
                string.IsNullOrWhiteSpace(action) ? null : $"action={Uri.EscapeDataString(action)}",
            }.Where(p => p is not null)));

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

    /// <summary>HU-18.1 (RF-105): descarga el .xlsx del período.</summary>
    public async Task<ExportFile> ExportAsync(DateOnly from, DateOnly to)
    {
        using var client = factory.CreateClient("gateway");
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/reports/export?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        if (session.Token is null) throw new ApiException(401, "La sesión no tiene conexión con el servidor.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new ApiException((int)response.StatusCode, await ReadProblemAsync(response, default));
        var name = response.Content.Headers.ContentDisposition?.FileNameStar
                   ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                   ?? $"farmacia-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx";
        return new ExportFile(await response.Content.ReadAsByteArrayAsync(), name,
            response.Headers.Contains("X-Export-Empty"));
    }
}
