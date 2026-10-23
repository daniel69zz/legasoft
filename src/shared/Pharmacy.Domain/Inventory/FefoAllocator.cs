using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Inventory;

public sealed record LotAllocation(Lot Lot, int Quantity);

/// <summary>
/// Regla FEFO (First Expired, First Out, RF-095): asigna la cantidad pedida a los
/// lotes vigentes que vencen primero. Los lotes vencidos nunca se asignan (RF-024).
/// </summary>
public static class FefoAllocator
{
    public static IReadOnlyList<LotAllocation> Allocate(IEnumerable<Lot> lots, int quantity, DateOnly today)
    {
        if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor a cero.");

        var available = lots
            .Where(l => !l.IsExpired(today) && l.Quantity > 0)
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.LotNumber, StringComparer.Ordinal)
            .ToList();

        var stock = available.Sum(l => l.Quantity);
        if (stock < quantity)
            throw new DomainException($"Stock insuficiente: se pidieron {quantity} y hay {stock} unidades vigentes.");

        var result = new List<LotAllocation>();
        var pending = quantity;
        foreach (var lot in available)
        {
            var take = Math.Min(lot.Quantity, pending);
            result.Add(new LotAllocation(lot, take));
            pending -= take;
            if (pending == 0) break;
        }
        return result;
    }

    /// <summary>Stock vendible: suma de los lotes no vencidos (RF-013, RF-039).</summary>
    public static int SellableStock(IEnumerable<Lot> lots, DateOnly today) =>
        lots.Where(l => !l.IsExpired(today)).Sum(l => l.Quantity);
}
