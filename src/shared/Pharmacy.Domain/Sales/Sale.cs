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

    // SCRUM-29 · HU-25 (RF-088, RF-089): datos de la receta asociada a la venta.
    public string PrescriptionDoctor { get; private set; } = "";
    public string PrescriptionPatient { get; private set; } = "";
    public DateOnly? PrescriptionIssuedOn { get; private set; }
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

    /// <summary>
    /// Cierra la venta con el efectivo recibido y calcula el cambio (RF-042, RF-044, RF-045). Si incluye
    /// medicamentos con receta, la receta es obligatoria y queda asociada a la venta (RF-089 a RF-091).
    /// </summary>
    public void Complete(decimal cashReceived, Prescription? prescription = null)
    {
        EnsureInProgress();
        if (_lines.Count == 0) throw new DomainException("La venta no tiene productos.");
        if (RequiresPrescription && prescription is null)
            throw new DomainException("La venta incluye medicamentos que requieren receta: registre la receta (RF-091).");
        // SCRUM-22 · HU-12: indica cuánto falta para cubrir el total.
        if (cashReceived < Total)
            throw new DomainException(
                $"El efectivo recibido (Bs {cashReceived:N2}) no cubre el total (Bs {Total:N2}): faltan Bs {MissingFor(cashReceived):N2}.");

        if (prescription is not null)
        {
            PrescriptionNumber = prescription.Number;
            PrescriptionDoctor = prescription.Doctor;
            PrescriptionPatient = prescription.Patient;
            PrescriptionIssuedOn = prescription.IssuedOn;
        }
        CashReceived = MoneyMath.Round(cashReceived);
        Change = MoneyMath.Round(CashReceived - Total);
        Status = SaleStatus.Completada;
    }

    /// <summary>SCRUM-22 · HU-12 (RF-044): lo que falta para cubrir el total con el efectivo recibido.</summary>
    public decimal MissingFor(decimal cashReceived) => Math.Max(0, MoneyMath.Round(Total - cashReceived));

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
