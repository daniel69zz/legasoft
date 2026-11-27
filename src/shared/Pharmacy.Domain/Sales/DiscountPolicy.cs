using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Sales;

/// <summary>
/// SCRUM-30 · HU-27 (RF-096): límite de los descuentos. Quién autoriza y el tope están pendientes de
/// confirmar con el cliente (observación 9 del análisis); mientras tanto el tope es configurable y por
/// defecto es el 20 % del subtotal.
/// </summary>
public static class DiscountPolicy
{
    public const int DefaultMaxPercent = 20;

    public static void EnsureWithinLimit(decimal discount, decimal subtotal, int maxPercent)
    {
        var max = MoneyMath.Round(subtotal * maxPercent / 100m);
        if (discount > max)
            throw new DomainException(
                $"El descuento (Bs {discount:N2}) supera el máximo autorizado del {maxPercent} % (Bs {max:N2}).");
    }
}
