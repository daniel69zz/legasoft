namespace Pharmacy.Application.Contracts;

public sealed record SaleLinePayload(Guid ProductId, string ProductName, Guid LotId, string LotNumber, int Quantity,
    decimal UnitPrice);

public sealed record SaleCreatedPayload(Guid SaleId, string ReceiptNumber, string DeviceId, string CashierName,
    Guid CashSessionId, DateTimeOffset CreatedAt, decimal Subtotal, decimal Discount, decimal Total,
    decimal CashReceived, decimal Change, string PrescriptionNumber, string CustomerName, string CustomerTaxId,
    IReadOnlyList<SaleLinePayload> Lines);

public sealed record SaleVoidedPayload(Guid SaleId, string Reason, string VoidedBy, DateTimeOffset VoidedAt);

public sealed record CashSessionOpenedPayload(Guid CashSessionId, string DeviceId, string CashierName,
    decimal OpeningAmount, DateTimeOffset OpenedAt);

public sealed record CashMovementPayload(Guid MovementId, Guid CashSessionId, string Type, decimal Amount,
    string Reason, string Category, string UserName, DateTimeOffset OccurredAt);

public sealed record CashSessionClosedPayload(Guid CashSessionId, string DeviceId, string CashierName,
    decimal OpeningAmount, decimal SalesTotal, decimal ExpensesTotal, decimal ExpectedAmount, decimal CountedAmount,
    decimal Difference, string Justification, DateTimeOffset OpenedAt, DateTimeOffset ClosedAt,
    IReadOnlyDictionary<decimal, int> Denominations);
