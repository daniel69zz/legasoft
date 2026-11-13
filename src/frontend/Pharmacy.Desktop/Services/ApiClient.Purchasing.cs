using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>HU-08 y HU-09: proveedores, compras y recepción en el Business Service.</summary>
public sealed partial class ApiClient
{
    public Task<List<SupplierDto>> GetSuppliersAsync(string search = "") =>
        SendAsync<List<SupplierDto>>(HttpMethod.Get, $"/api/suppliers?search={Uri.EscapeDataString(search)}");

    public Task<SupplierDto> CreateSupplierAsync(SaveSupplierRequest request) =>
        SendAsync<SupplierDto>(HttpMethod.Post, "/api/suppliers", request);

    public Task<SupplierDto> UpdateSupplierAsync(Guid id, SaveSupplierRequest request) =>
        SendAsync<SupplierDto>(HttpMethod.Put, $"/api/suppliers/{id}", request);

    public Task SetSupplierActiveAsync(Guid id, bool active) =>
        SendAsync<object>(HttpMethod.Post, $"/api/suppliers/{id}/{(active ? "activate" : "deactivate")}");
}
