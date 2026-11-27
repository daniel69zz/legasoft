using Pharmacy.Domain.Common;

namespace Pharmacy.Domain.Sales;

/// <summary>
/// SCRUM-29 · HU-25 (RF-088 a RF-091): receta médica registrada y asociada a la venta.
/// Regla de validez provisional, pendiente de confirmar con el cliente (observación 9 del análisis):
/// número, médico y paciente obligatorios, y fecha de emisión no futura ni más antigua que
/// <see cref="ValidityDays"/> días.
/// </summary>
public sealed record Prescription(string Number, string Doctor, string Patient, DateOnly IssuedOn)
{
    public const int ValidityDays = 30;

    public static Prescription Create(string? number, string? doctor, string? patient, DateOnly? issuedOn,
        DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("La venta incluye medicamentos que requieren receta: registre la receta (RF-091).");
        if (string.IsNullOrWhiteSpace(doctor)) throw new DomainException("Indique el médico que emitió la receta (RF-088).");
        if (string.IsNullOrWhiteSpace(patient)) throw new DomainException("Indique el paciente de la receta (RF-088).");
        if (issuedOn is not { } date) throw new DomainException("Indique la fecha de emisión de la receta (RF-088).");
        if (date > today) throw new DomainException("La fecha de la receta no puede ser futura.");
        if (date < today.AddDays(-ValidityDays))
            throw new DomainException($"La receta venció: tiene más de {ValidityDays} días desde su emisión.");
        return new Prescription(number.Trim(), doctor.Trim(), patient.Trim(), date);
    }
}
