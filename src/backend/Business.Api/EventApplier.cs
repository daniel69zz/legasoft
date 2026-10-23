using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Inventory;

namespace Business.Api;

/// <summary>
/// Aplica en PostgreSQL los eventos despachados por el Sync Service. La venta ya
/// fue validada en el puesto con las mismas reglas de Pharmacy.Domain; aquí se
/// consolida y se descuenta el stock central del lote vendido.
/// </summary>
public class EventApplier(BusinessDb db, ILogger<EventApplier> logger)
{
    public async Task ApplyAsync(SyncEnvelope message, CancellationToken ct)
    {
        switch (message.EventType)
        {
            case EventTypes.SaleCreated:
                await SaleCreatedAsync(Read<SaleCreatedPayload>(message), message, ct);
                break;
            case EventTypes.SaleVoided:
                await SaleVoidedAsync(Read<SaleVoidedPayload>(message), message, ct);
                break;
            case EventTypes.CashSessionOpened:
                var opened = Read<CashSessionOpenedPayload>(message);
                if (await db.CashSessions.FindAsync([opened.CashSessionId], ct) is null)
                    db.CashSessions.Add(new CashSessionRecord
                    {
                        Id = opened.CashSessionId, DeviceId = opened.DeviceId, CashierName = opened.CashierName,
                        OpeningAmount = opened.OpeningAmount, OpenedAt = opened.OpenedAt,
                    });
                break;
            case EventTypes.CashMovementRegistered:
                var m = Read<CashMovementPayload>(message);
                db.CashMovements.Add(new CashMovementRecord
                {
                    Id = m.MovementId, CashSessionId = m.CashSessionId, Type = m.Type, Amount = m.Amount,
                    Reason = m.Reason, Category = m.Category, UserName = m.UserName, OccurredAt = m.OccurredAt,
                });
                Audit(message, m.UserName, "Egreso de caja", $"Bs {m.Amount:0.00} · {m.Category} · {m.Reason}");
                break;
            case EventTypes.CashSessionClosed:
                await CashClosedAsync(Read<CashSessionClosedPayload>(message), message, ct);
                break;
            default:
                throw new ArgumentException($"Tipo de evento desconocido: {message.EventType}");
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SaleCreatedAsync(SaleCreatedPayload p, SyncEnvelope message, CancellationToken ct)
    {
        if (await db.Sales.AnyAsync(s => s.Id == p.SaleId, ct)) return;

        db.Sales.Add(new SaleRecord
        {
            Id = p.SaleId, ReceiptNumber = p.ReceiptNumber, DeviceId = p.DeviceId, CashierName = p.CashierName,
            CashSessionId = p.CashSessionId, CreatedAt = p.CreatedAt, Subtotal = p.Subtotal, Discount = p.Discount,
            Total = p.Total, CashReceived = p.CashReceived, Change = p.Change,
            PrescriptionNumber = p.PrescriptionNumber, CustomerName = p.CustomerName,
            CustomerTaxId = p.CustomerTaxId,
            Lines = p.Lines.Select(l => new SaleLineRecord
            {
                Id = Guid.NewGuid(), SaleId = p.SaleId, ProductId = l.ProductId, ProductName = l.ProductName,
                LotId = l.LotId, LotNumber = l.LotNumber, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            }).ToList(),
        });
        db.CashMovements.Add(new CashMovementRecord
        {
            Id = Guid.NewGuid(), CashSessionId = p.CashSessionId, Type = "Venta", Amount = p.Total,
            Reason = $"Venta {p.ReceiptNumber}", Category = "Ventas", UserName = p.CashierName,
            OccurredAt = p.CreatedAt,
        });
        if (p.Discount > 0)
            Audit(message, p.CashierName, "Descuento aplicado", $"{p.ReceiptNumber}: Bs {p.Discount:0.00}");

        await MoveStockAsync(p.Lines.Select(l => (l.ProductId, l.LotId, -l.Quantity)), StockMovementType.Venta,
            $"Venta {p.ReceiptNumber}", p.CashierName, p.CreatedAt, ct);
    }

    private async Task SaleVoidedAsync(SaleVoidedPayload p, SyncEnvelope message, CancellationToken ct)
    {
        var sale = await db.Sales.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == p.SaleId, ct)
            ?? throw new ArgumentException("La venta anulada no existe en el servidor.");
        if (sale.Status == "Anulada") return;

        sale.Status = "Anulada";
        sale.VoidReason = p.Reason;
        db.CashMovements.Add(new CashMovementRecord
        {
            Id = Guid.NewGuid(), CashSessionId = sale.CashSessionId, Type = "AnulacionVenta", Amount = sale.Total,
            Reason = p.Reason, Category = "Anulaciones", UserName = p.VoidedBy, OccurredAt = p.VoidedAt,
        });
        await MoveStockAsync(sale.Lines.Select(l => (l.ProductId, l.LotId, l.Quantity)), StockMovementType.Ajuste,
            $"Anulación {sale.ReceiptNumber}", p.VoidedBy, p.VoidedAt, ct);
        Audit(message, p.VoidedBy, "Anulación de venta", $"{sale.ReceiptNumber}: {p.Reason}");
    }

    private async Task CashClosedAsync(CashSessionClosedPayload p, SyncEnvelope message, CancellationToken ct)
    {
        var session = await db.CashSessions.FindAsync([p.CashSessionId], ct);
        if (session is null)
            db.CashSessions.Add(session = new CashSessionRecord
            {
                Id = p.CashSessionId, DeviceId = p.DeviceId, CashierName = p.CashierName, OpenedAt = p.OpenedAt,
                OpeningAmount = p.OpeningAmount,
            });
        session.Status = "Cerrada";
        session.SalesTotal = p.SalesTotal;
        session.ExpensesTotal = p.ExpensesTotal;
        session.ExpectedAmount = p.ExpectedAmount;
        session.CountedAmount = p.CountedAmount;
        session.Difference = p.Difference;
        session.Justification = p.Justification;
        session.ClosedAt = p.ClosedAt;
        Audit(message, p.CashierName, "Cierre de caja",
            $"Esperado Bs {p.ExpectedAmount:0.00}, contado Bs {p.CountedAmount:0.00}, diferencia Bs {p.Difference:0.00}");
    }

    private async Task MoveStockAsync(IEnumerable<(Guid ProductId, Guid LotId, int Quantity)> moves,
        StockMovementType type, string reason, string userName, DateTimeOffset at, CancellationToken ct)
    {
        foreach (var (productId, lotId, quantity) in moves)
        {
            var lot = await db.Lots.FindAsync([lotId], ct);
            if (lot is null)
            {
                logger.LogWarning("Lote {LotId} desconocido en el servidor; se registra solo el movimiento", lotId);
            }
            else if (quantity > 0)
            {
                lot.Increase(quantity);
            }
            else
            {
                // Otro puesto pudo vender el mismo lote sin conexión: se registra la diferencia, no se rechaza la venta.
                var take = Math.Min(lot.Quantity, -quantity);
                if (take > 0) lot.Decrease(take);
                if (take < -quantity)
                    logger.LogWarning("Stock central insuficiente en el lote {Lot}: faltan {Missing} unidades",
                        lot.LotNumber, -quantity - take);
            }
            db.StockMovements.Add(new StockMovement(Guid.NewGuid(), productId, lotId, type, quantity, reason, userName,
                at));
        }
    }

    private void Audit(SyncEnvelope message, string userName, string action, string detail) =>
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(), OccurredAt = message.Timestamp, UserName = userName, DeviceId = message.DeviceId,
            Action = action, Detail = detail, EventId = message.EventId,
        });

    private static T Read<T>(SyncEnvelope message) =>
        JsonSerializer.Deserialize<T>(message.Payload, OutboxMessage.Json)
        ?? throw new ArgumentException($"Payload vacío en {message.EventType}.");
}
