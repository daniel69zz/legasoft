using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Cash;

public enum CashSessionStatus
{
    Abierta,
    Cerrada,
    EntregaPendiente,
    Entregada,
}

/// <summary>
/// Turno de caja de un puesto: apertura, movimientos, cierre con conteo por
/// denominación y entrega al cajero entrante (RF-052 a RF-062, RF-106).
/// </summary>
public class CashSession
{
    private readonly List<CashMovement> _movements = [];

    private CashSession() { }

    public static CashSession Open(Guid id, string deviceId, string cashierName, decimal openingAmount,
        DateTimeOffset now)
    {
        if (openingAmount < 0) throw new DomainException("El monto inicial no puede ser negativo.");
        return new CashSession
        {
            Id = id,
            DeviceId = deviceId,
            CashierName = cashierName,
            OpeningAmount = MoneyMath.Round(openingAmount),
            OpenedAt = now,
            Status = CashSessionStatus.Abierta,
        };
    }

    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = "";
    public string CashierName { get; private set; } = "";
    public DateTimeOffset OpenedAt { get; private set; }
    public decimal OpeningAmount { get; private set; }
    public CashSessionStatus Status { get; private set; }
    public decimal? CountedAmount { get; private set; }
    public string Justification { get; private set; } = "";
    public DateTimeOffset? ClosedAt { get; private set; }
    public string HandoverTo { get; private set; } = "";
    public decimal? HandoverReceivedAmount { get; private set; }
    public DateTimeOffset? HandoverConfirmedAt { get; private set; }

    public IReadOnlyList<CashMovement> Movements => _movements;

    public decimal SalesTotal => _movements.Where(m => m.Type == CashMovementType.Venta).Sum(m => m.Amount);
    public decimal ExpensesTotal => _movements.Where(m => m.Type != CashMovementType.Venta).Sum(m => m.Amount);

    /// <summary>RF-057: monto inicial + ventas − egresos y anulaciones.</summary>
    public decimal ExpectedAmount => MoneyMath.Round(OpeningAmount + _movements.Sum(m => m.SignedAmount));

    /// <summary>RF-059: contado − esperado. Positivo es sobrante y negativo es faltante.</summary>
    public decimal? Difference => CountedAmount is null ? null : MoneyMath.Round(CountedAmount.Value - ExpectedAmount);

    public CashMovement RegisterSale(Guid saleId, decimal amount, string userName, DateTimeOffset now)
    {
        EnsureOpen();
        if (amount <= 0) throw new DomainException("El ingreso por venta debe ser mayor a cero.");
        return Add(CashMovementType.Venta, amount, "Venta", "Ventas", userName, now, saleId);
    }

    public CashMovement RegisterSaleVoid(Guid saleId, decimal amount, string reason, string userName,
        DateTimeOffset now)
    {
        EnsureOpen();
        return Add(CashMovementType.AnulacionVenta, amount, reason, "Anulaciones", userName, now, saleId);
    }

    /// <summary>RF-055: egreso con motivo y categoría; no puede dejar la caja en negativo.</summary>
    public CashMovement RegisterExpense(decimal amount, string reason, string category, string userName,
        DateTimeOffset now)
    {
        EnsureOpen();
        if (amount <= 0) throw new DomainException("El egreso debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Indique el motivo del egreso.");
        if (string.IsNullOrWhiteSpace(category)) throw new DomainException("Indique la categoría del egreso.");
        if (amount > ExpectedAmount)
            throw new DomainException($"El egreso supera el efectivo disponible en caja (Bs {ExpectedAmount:0.00}).");
        return Add(CashMovementType.Egreso, amount, reason.Trim(), category.Trim(), userName, now, null);
    }

    /// <summary>RF-058, RF-059 y RF-106: cierre con conteo y justificación obligatoria si hay diferencia.</summary>
    public void Close(CashCount count, string? justification, DateTimeOffset now)
    {
        EnsureOpen();
        var difference = MoneyMath.Round(count.Total - ExpectedAmount);
        if (difference != 0 && string.IsNullOrWhiteSpace(justification))
            throw new DomainException(
                $"Hay una diferencia de Bs {difference:0.00}: registre la justificación antes de cerrar (RF-106).");

        CountedAmount = count.Total;
        Justification = justification?.Trim() ?? "";
        ClosedAt = now;
        Status = CashSessionStatus.Cerrada;
    }

    /// <summary>RF-061: la entrega queda pendiente hasta que el cajero entrante confirme.</summary>
    public void RequestHandover(string incomingCashier)
    {
        if (Status != CashSessionStatus.Cerrada) throw new DomainException("Cierre la caja antes de entregarla.");
        if (string.IsNullOrWhiteSpace(incomingCashier)) throw new DomainException("Indique el cajero entrante.");
        if (incomingCashier == CashierName) throw new DomainException("El cajero entrante debe ser otra persona.");
        HandoverTo = incomingCashier;
        Status = CashSessionStatus.EntregaPendiente;
    }

    /// <summary>RF-062: solo se completa con el monto recibido y la conformidad del cajero entrante.</summary>
    public void ConfirmHandover(string incomingCashier, decimal receivedAmount, bool agrees, DateTimeOffset now)
    {
        if (Status != CashSessionStatus.EntregaPendiente) throw new DomainException("No hay una entrega pendiente.");
        if (incomingCashier != HandoverTo) throw new DomainException("Solo el cajero entrante puede confirmar.");
        HandoverReceivedAmount = MoneyMath.Round(receivedAmount);
        if (!agrees || HandoverReceivedAmount != CountedAmount) return; // sigue pendiente
        HandoverConfirmedAt = now;
        Status = CashSessionStatus.Entregada;
    }

    private CashMovement Add(CashMovementType type, decimal amount, string reason, string category, string userName,
        DateTimeOffset now, Guid? saleId)
    {
        var movement = new CashMovement(Guid.NewGuid(), Id, type, MoneyMath.Round(amount), reason, category, userName,
            now, saleId);
        _movements.Add(movement);
        return movement;
    }

    private void EnsureOpen()
    {
        if (Status != CashSessionStatus.Abierta) throw new DomainException("La caja no está abierta.");
    }
}
