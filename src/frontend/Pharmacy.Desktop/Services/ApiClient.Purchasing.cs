using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>SCRUM-18 · HU-08 y SCRUM-19 · HU-09: proveedores, compras y recepción en el Business Service.</summary>
public sealed partial class ApiClient
{
    // SCRUM-18 · HU-08
    public Task<List<SupplierDto>> GetSuppliersAsync(string search = "") =>
        SendAsync<List<SupplierDto>>(HttpMethod.Get, $"/api/suppliers?search={Uri.EscapeDataString(search)}");

    public Task<SupplierDto> CreateSupplierAsync(SaveSupplierRequest request) =>
        SendAsync<SupplierDto>(HttpMethod.Post, "/api/suppliers", request);

    public Task<SupplierDto> UpdateSupplierAsync(Guid id, SaveSupplierRequest request) =>
        SendAsync<SupplierDto>(HttpMethod.Put, $"/api/suppliers/{id}", request);

    public Task SetSupplierActiveAsync(Guid id, bool active) =>
        SendAsync<object>(HttpMethod.Post, $"/api/suppliers/{id}/{(active ? "activate" : "deactivate")}");

    public Task<List<SupplierProductDto>> GetSupplierProductsAsync(Guid id) =>
        SendAsync<List<SupplierProductDto>>(HttpMethod.Get, $"/api/suppliers/{id}/products");

    public Task<List<PurchaseDto>> GetSupplierPurchasesAsync(Guid id) =>
        SendAsync<List<PurchaseDto>>(HttpMethod.Get, $"/api/suppliers/{id}/purchases");

    // SCRUM-19 · HU-09
    public Task<List<PurchaseDto>> GetPurchasesAsync() =>
        SendAsync<List<PurchaseDto>>(HttpMethod.Get, "/api/purchases");

    public Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request) =>
        SendAsync<PurchaseDto>(HttpMethod.Post, "/api/purchases", request);

    public Task<PurchaseDto> ReceivePurchaseAsync(Guid id, ReceivePurchaseRequest request) =>
        SendAsync<PurchaseDto>(HttpMethod.Post, $"/api/purchases/{id}/receive", request);

    // SCRUM-19 · HU-09 y SCRUM-112
    public Task<LotPullResponse> PullLotsAsync(long since, bool snapshot, CancellationToken ct) =>
        SendAsync<LotPullResponse>(HttpMethod.Get,
            $"/sync/pull-lots?since={since}&snapshot={(snapshot ? "true" : "false")}", ct: ct);
}
