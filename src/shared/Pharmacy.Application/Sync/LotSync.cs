using Pharmacy.Application.Contracts;
using Pharmacy.Domain.Inventory;

namespace Pharmacy.Application.Sync;

/// <summary>Resultado de aplicar en el puesto los lotes descargados del servidor.</summary>
public sealed record LotMergeResult(IReadOnlyList<Lot> Added, int Updated);

/// <summary>
/// Mezcla en el puesto los lotes que envía el servidor (HU-09 y SCRUM-112). Los lotes nuevos se
/// agregan. En los que el puesto ya tiene, el número y el vencimiento se toman del servidor (las
/// semillas de cada base calculan las fechas desde el día en que se crearon, y la regla RF-024 de
/// no vender vencidos debe usar la fecha oficial). La cantidad solo se toma del servidor si el
/// puesto no registró movimientos en ese lote: si ya vendió o ajustó, manda su stock local, porque
/// esos movimientos todavía pueden estar viajando al servidor.
/// </summary>
public static class LotSync
{
    public static LotMergeResult Merge(IReadOnlyDictionary<Guid, Lot> local, IReadOnlySet<Guid> lotsWithLocalMovements,
        IEnumerable<LotChangeDto> changes)
    {
        var added = new List<Lot>();
        var updated = 0;
        foreach (var c in changes)
        {
            if (!local.TryGetValue(c.LotId, out var lot))
            {
                added.Add(new Lot(c.LotId, c.ProductId, c.LotNumber, c.ExpirationDate, c.Quantity));
                continue;
            }
            var quantity = lotsWithLocalMovements.Contains(c.LotId) ? (int?)null : c.Quantity;
            if (lot.LotNumber == c.LotNumber && lot.ExpirationDate == c.ExpirationDate
                && (quantity is null || lot.Quantity == quantity)) continue;
            lot.ApplyServerData(c.LotNumber, c.ExpirationDate, quantity);
            updated++;
        }
        return new LotMergeResult(added, updated);
    }
}
