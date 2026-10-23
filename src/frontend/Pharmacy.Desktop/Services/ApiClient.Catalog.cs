using Pharmacy.Application.Contracts;

namespace Pharmacy.Desktop.Services;

/// <summary>HU-05: catálogo central de medicamentos en el Business Service.</summary>
public sealed partial class ApiClient
{
    public Task<List<ProductDto>> SearchCatalogAsync(string search) =>
        SendAsync<List<ProductDto>>(HttpMethod.Get, $"/api/catalog?search={Uri.EscapeDataString(search)}");

    public Task<ProductDto> CreateProductAsync(SaveProductRequest request) =>
        SendAsync<ProductDto>(HttpMethod.Post, "/api/catalog", request);

    public Task<ProductDto> UpdateProductAsync(Guid id, SaveProductRequest request) =>
        SendAsync<ProductDto>(HttpMethod.Put, $"/api/catalog/{id}", request);

    public Task SetProductActiveAsync(Guid id, bool active) =>
        SendAsync<object>(HttpMethod.Post, $"/api/catalog/{id}/{(active ? "activate" : "deactivate")}");
}
