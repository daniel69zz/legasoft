using System.Text.Json;
using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Sync;

/// <summary>Resultado de aplicar en el puesto los lotes descargados del servidor.</summary>
public sealed record LotMergeResult(IReadOnlyList<Lot> Added, int Updated);

/// <summary>
/// Mezcla en el puesto los lotes que envía el servidor (SCRUM-19 · HU-09, SCRUM-112 y SCRUM-35 · HU-22).
/// Los lotes nuevos se agregan. En los que el puesto ya tiene, el número y el vencimiento se toman del
/// servidor (la regla RF-024 de no vender vencidos debe usar la fecha oficial) y el stock es el del
/// servidor más los movimientos del puesto que todavía no llegaron a él. Así dos puestos que venden el
/// mismo lote terminan viendo el mismo stock en cuanto sincronizan.
/// </summary>
public static class LotSync
{
    public static LotMergeResult Merge(IReadOnlyDictionary<Guid, Lot> local,
        IReadOnlyDictionary<Guid, int> pendingDeltas, IEnumerable<LotChangeDto> changes)
    {
        var added = new List<Lot>();
        var updated = 0;
        // Un mismo lote puede llegar varias veces en una descarga (una por cada cambio de stock): vale el último.
        foreach (var c in changes.GroupBy(c => c.LotId).Select(g => g.MaxBy(c => c.Sequence)!))
        {
            var quantity = Math.Max(0, c.Quantity + pendingDeltas.GetValueOrDefault(c.LotId));
            if (!local.TryGetValue(c.LotId, out var lot))
            {
                added.Add(new Lot(c.LotId, c.ProductId, c.LotNumber, c.ExpirationDate, quantity));
                continue;
            }
            if (lot.LotNumber == c.LotNumber && lot.ExpirationDate == c.ExpirationDate && lot.Quantity == quantity)
                continue;
            lot.ApplyServerData(c.LotNumber, c.ExpirationDate, quantity);
            updated++;
        }
        return new LotMergeResult(added, updated);
    }
}

/// <summary>
/// SCRUM-35 · HU-22: efecto en el stock de cada lote de los mensajes del Outbox que el servidor todavía
/// no aplicó (ventas, anulaciones y movimientos manuales). Se suma a la cantidad que envía el servidor.
/// </summary>
public static class PendingLotDeltas
{
    /// <param name="pending">Mensajes del Outbox no sincronizados.</param>
    /// <param name="saleLines">Líneas (lote y cantidad) de una venta local, para las anulaciones pendientes.</param>
    public static IReadOnlyDictionary<Guid, int> From(IEnumerable<OutboxMessage> pending,
        Func<Guid, IEnumerable<(Guid LotId, int Quantity)>> saleLines)
    {
        var deltas = new Dictionary<Guid, int>();
        void Add(Guid lotId, int quantity) => deltas[lotId] = deltas.GetValueOrDefault(lotId) + quantity;

        foreach (var m in pending)
        {
            switch (m.EventType)
            {
                case EventTypes.SaleCreated:
                    foreach (var l in Read<SaleCreatedPayload>(m).Lines) Add(l.LotId, -l.Quantity);
                    break;
                case EventTypes.SaleVoided:
                    foreach (var (lotId, quantity) in saleLines(Read<SaleVoidedPayload>(m).SaleId)) Add(lotId, quantity);
                    break;
                case EventTypes.StockMovementCreated:
                    var movement = Read<StockMovementPayload>(m);
                    Add(movement.LotId, movement.Quantity);
                    break;
            }
        }
        return deltas;
    }

    private static T Read<T>(OutboxMessage m) => JsonSerializer.Deserialize<T>(m.Payload, OutboxMessage.Json)!;
}
