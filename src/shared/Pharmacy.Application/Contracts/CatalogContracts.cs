namespace Pharmacy.Application.Contracts;

/// <summary>Medicamento tal como lo administra el catálogo central (HU-05, RF-009 a RF-012).</summary>
public sealed record ProductDto(Guid Id, string Code, string Name, string ActiveIngredient, string Concentration,
    string Presentation, string Laboratory, string Category, bool RequiresPrescription, decimal SalePrice,
    int MinimumStock, string Location, bool IsActive);

/// <summary>Datos para registrar (RF-009) o modificar (RF-010) un medicamento. Al modificar, el código se ignora.</summary>
public sealed record SaveProductRequest(string Code, string Name, string ActiveIngredient, string Concentration,
    string Presentation, string Laboratory, string Category, bool RequiresPrescription, decimal SalePrice,
    int MinimumStock, string Location);
