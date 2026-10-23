using Pharmacy.Domain.Catalog;
using Pharmacy.Domain.Common;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Domain.Sales;

public enum SaleStatus
{
    EnCurso,
    Completada,
    Anulada,
}

/// <summary>
/// Venta con pago en efectivo (RF-037 a RF-049, RF-091, RF-096). Concentra las
/// reglas de cálculo para que el puesto y el servidor obtengan el mismo resultado.
/// </summary>
public class Sale
{
    private readonly List<SaleLine> _lines = [];

    private Sale() { }

    public static Sale Start(Guid id, string receiptNumber, string deviceId, string cashierName, Guid cashSessionId,
        DateTimeOffset now) => new()
    {
        Id = id,
        ReceiptNumber = receiptNumber,
        DeviceId = deviceId,
        CashierName = cashierName,
        CashSessionId = cashSessionId,
        CreatedAt = now,
        Status = SaleStatus.EnCurso,
    };

    public Guid Id { get; private set; }
    public string ReceiptNumber { get; private set; } = "";
    public string DeviceId { get; private set; } = "";
    public string CashierName { get; private set; } = "";
    public Guid CashSessionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public SaleStatus Status { get; private set; }
    public decimal Discount { get; private set; }
    public string DiscountAuthorizedBy { get; private set; } = "";
    public decimal CashReceived { get; private set; }
    public decimal Change { get; private set; }
    public string PrescriptionNumber { get; private set; } = "";
    public string CustomerName { get; private set; } = "";
    public string CustomerTaxId { get; private set; } = "";
    public string VoidReason { get; private set; } = "";
    public string VoidedBy { get; private set; } = "";
    public DateTimeOffset? VoidedAt { get; private set; }

    public IReadOnlyList<SaleLine> Lines => _lines;
    public decimal Subtotal => MoneyMath.Round(_lines.Sum(l => l.Subtotal));
    public decimal Total => MoneyMath.Round(Subtotal - Discount);
    public bool RequiresPrescription => _lines.Any(l => l.RequiresPrescription);

    /// <summary>Agrega un producto ya asignado a lotes por FEFO.</summary>
    public void AddLine(Product product, IReadOnlyList<LotAllocation> allocations)
    {
        EnsureInProgress();
        if (!product.IsActive) throw new DomainException($"{product.Name} está desactivado y no se puede vender.");
        if (allocations.Count == 0) throw new DomainException("No hay lotes asignados para el producto.");

        foreach (var a in allocations)
        {
            if (a.Lot.ProductId != product.Id) throw new DomainException("El lote no corresponde al producto.");
            _lines.Add(new SaleLine(Guid.NewGuid(), Id, product.Id, product.DisplayName, a.Lot.Id, a.Lot.LotNumber,
                a.Quantity, product.SalePrice, product.RequiresPrescription));
        }
    }

    /// <summary>RF-096: descuento autorizado con el usuario responsable.</summary>
    public void ApplyDiscount(decimal amount, string authorizedBy)
    {
        EnsureInProgress();
        if (amount < 0) throw new DomainException("El descuento no puede ser negativo.");
        if (amount > Subtotal) throw new DomainException("El descuento no puede superar el subtotal.");
        if (amount > 0 && string.IsNullOrWhiteSpace(authorizedBy))
            throw new DomainException("El descuento requiere el usuario que lo autoriza.");
        Discount = MoneyMath.Round(amount);
        DiscountAuthorizedBy = authorizedBy;
    }

    public void SetCustomer(string name, string taxId)
    {
        CustomerName = name.Trim();
        CustomerTaxId = taxId.Trim();
    }

    /// <summary>Cierra la venta con el efectivo recibido y calcula el cambio (RF-042, RF-044, RF-045).</summary>
    public void Complete(decimal cashReceived, string? prescriptionNumber)
    {
        EnsureInProgress();
        if (_lines.Count == 0) throw new DomainException("La venta no tiene productos.");
        if (RequiresPrescription && string.IsNullOrWhiteSpace(prescriptionNumber))
            throw new DomainException("La venta incluye medicamentos que requieren receta: registre la receta (RF-091).");
        if (cashReceived < Total)
            throw new DomainException($"El efectivo recibido (Bs {cashReceived:0.00}) no cubre el total (Bs {Total:0.00}).");

        PrescriptionNumber = prescriptionNumber?.Trim() ?? "";
        CashReceived = MoneyMath.Round(cashReceived);
        Change = MoneyMath.Round(CashReceived - Total);
        Status = SaleStatus.Completada;
    }

    /// <summary>RF-048: solo se anula una venta completada y con motivo.</summary>
    public void Void(string reason, string userName, DateTimeOffset now)
    {
        if (Status != SaleStatus.Completada) throw new DomainException("Solo se puede anular una venta completada.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Indique el motivo de la anulación.");
        Status = SaleStatus.Anulada;
        VoidReason = reason.Trim();
        VoidedBy = userName;
        VoidedAt = now;
    }

    private void EnsureInProgress()
    {
        if (Status != SaleStatus.EnCurso) throw new DomainException("La venta ya fue cerrada.");
    }
}
