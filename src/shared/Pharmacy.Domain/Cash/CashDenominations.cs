using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Cash;

/// <summary>Billetes y monedas en circulación del boliviano (RF-054).</summary>
public static class CashDenominations
{
    public static readonly IReadOnlyList<decimal> All = [200m, 100m, 50m, 20m, 10m, 5m, 2m, 1m, 0.50m, 0.20m, 0.10m];

    public static bool IsBill(decimal value) => value >= 10m;
}

/// <summary>Conteo físico del efectivo por denominación.</summary>
public sealed class CashCount
{
    private readonly Dictionary<decimal, int> _units = [];

    public CashCount(IReadOnlyDictionary<decimal, int> units)
    {
        foreach (var (denomination, count) in units)
        {
            if (!CashDenominations.All.Contains(denomination))
                throw new DomainException($"Bs {denomination:0.00} no es una denominación válida.");
            if (count < 0) throw new DomainException("La cantidad de billetes o monedas no puede ser negativa.");
            if (count > 0) _units[denomination] = count;
        }
    }

    public IReadOnlyDictionary<decimal, int> Units => _units;
    public decimal Total => MoneyMath.Round(_units.Sum(u => u.Key * u.Value));
}
