namespace Pharmacy.Domain.Common;

public static class MoneyMath
{
    /// <summary>Redondeo comercial a centavos (Bs 0,01).</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
