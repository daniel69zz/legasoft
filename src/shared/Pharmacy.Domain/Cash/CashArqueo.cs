namespace Pharmacy.Domain.Cash;

/// <summary>
/// SCRUM-25 · HU-15 (RF-056): arqueo hecho durante el turno. Registra lo contado frente a lo esperado en
/// ese momento, sin cerrar la caja ni cambiar su saldo.
/// </summary>
public class CashArqueo
{
    private CashArqueo() { }

    internal CashArqueo(Guid id, Guid cashSessionId, decimal expectedAmount, decimal countedAmount, string note,
        string userName, DateTimeOffset occurredAt)
    {
        Id = id;
        CashSessionId = cashSessionId;
        ExpectedAmount = expectedAmount;
        CountedAmount = countedAmount;
        Note = note;
        UserName = userName;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid CashSessionId { get; private set; }
    public decimal ExpectedAmount { get; private set; }
    public decimal CountedAmount { get; private set; }
    public string Note { get; private set; } = "";
    public string UserName { get; private set; } = "";
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Contado − esperado: positivo es sobrante y negativo es faltante.</summary>
    public decimal Difference => CountedAmount - ExpectedAmount;
}
