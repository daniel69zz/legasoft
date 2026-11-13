namespace Pharmacy.Application.Contracts;

/// <summary>Proveedor tal como lo administra el Business Service (SCRUM-18 · HU-08).</summary>
public sealed record SupplierDto(Guid Id, string TaxId, string Name, string ContactName, string Phone, string Email,
    string Address, bool IsActive);

/// <summary>Datos para registrar (RF-026) o modificar (RF-027) un proveedor. Al modificar, el NIT se ignora.</summary>
public sealed record SaveSupplierRequest(string TaxId, string Name, string ContactName, string Phone, string Email,
    string Address);

/// <summary>RF-028: producto que el proveedor suministró, según sus compras.</summary>
public sealed record SupplierProductDto(Guid ProductId, string ProductName, int TotalQuantity, decimal LastUnitCost,
    DateTimeOffset LastPurchaseAt);

public sealed record PurchaseLineDto(Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitCost,
    decimal Subtotal, int ReceivedQuantity, int DamagedQuantity, int MissingQuantity, int Difference,
    string LotNumber, DateOnly? ExpirationDate);

/// <summary>Compra con sus líneas; tras la recepción incluye lo recibido, dañado y faltante (RF-107, RF-108).</summary>
public sealed record PurchaseDto(Guid Id, string Number, Guid SupplierId, string SupplierName, string Status,
    DateTimeOffset CreatedAt, string CreatedBy, decimal Total, decimal ReceivedTotal, DateTimeOffset? ReceivedAt,
    string ReceivedBy, IReadOnlyList<PurchaseLineDto> Lines);

public sealed record PurchaseItemRequest(Guid ProductId, int Quantity, decimal UnitCost);

/// <summary>RF-030: compra a un proveedor con sus productos, cantidades y costos.</summary>
public sealed record CreatePurchaseRequest(Guid SupplierId, IReadOnlyList<PurchaseItemRequest> Items);

public sealed record ReceptionLineRequest(Guid LineId, string LotNumber, DateOnly? ExpirationDate,
    int ReceivedQuantity, int DamagedQuantity);

/// <summary>RF-032: recepción de una compra con lote, vencimiento y unidades en buen estado o dañadas.</summary>
public sealed record ReceivePurchaseRequest(IReadOnlyList<ReceptionLineRequest> Lines);

/// <summary>Lote creado en el servidor (recepción de compras) que los puestos deben descargar.</summary>
public sealed record LotChangeDto(long Sequence, Guid LotId, Guid ProductId, string LotNumber, DateOnly ExpirationDate,
    int Quantity);

/// <summary>Lotes nuevos desde una versión (descarga incremental, pendiente de HU-22.1).</summary>
public sealed record LotPullResponse(long Version, IReadOnlyList<LotChangeDto> Lots);

/// <summary>SCRUM-124 · HU-10.1 (RF-034, RF-035): precio ofrecido por un proveedor para un producto.</summary>
public sealed record SupplierPriceDto(Guid SupplierId, string SupplierName, Guid ProductId, string ProductName,
    decimal Price, DateTimeOffset RegisteredAt, string RegisteredBy);

public sealed record RegisterSupplierPriceRequest(Guid ProductId, decimal Price);
