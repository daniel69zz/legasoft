namespace Pharmacy.Application.Contracts;

/// <summary>Proveedor tal como lo administra el Business Service (HU-08).</summary>
public sealed record SupplierDto(Guid Id, string TaxId, string Name, string ContactName, string Phone, string Email,
    string Address, bool IsActive);

/// <summary>Datos para registrar (RF-026) o modificar (RF-027) un proveedor. Al modificar, el NIT se ignora.</summary>
public sealed record SaveSupplierRequest(string TaxId, string Name, string ContactName, string Phone, string Email,
    string Address);
