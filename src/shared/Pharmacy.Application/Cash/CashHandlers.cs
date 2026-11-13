using Pharmacy.Application.Abstractions;
using Pharmacy.Application.Contracts;
using Pharmacy.Application.Sync;
using Pharmacy.Domain.Cash;
using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Cash;

/// <summary>Caso de uso CashLocalService: apertura, egresos y cierre de caja del puesto.</summary>
public sealed class CashHandlers(
    ICashRepository cash,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser user)
{
    public Task<CashSession?> GetOpenAsync(CancellationToken ct = default) => cash.GetOpenAsync(user.DeviceId, ct);

    /// <summary>Último turno del puesto, para mostrar la entrega pendiente o la que se puede iniciar.</summary>
    public Task<CashSession?> GetLatestAsync(CancellationToken ct = default) => cash.GetLatestAsync(user.DeviceId, ct);

    /// <summary>RF-052. Con una entrega pendiente (RF-062) nadie abre caja en el puesto hasta confirmarla.</summary>
    public async Task<CashSession> OpenAsync(decimal openingAmount, CancellationToken ct = default)
    {
        if (await cash.GetOpenAsync(user.DeviceId, ct) is not null)
            throw new DomainException("Ya hay una caja abierta en este puesto.");
        if (await cash.GetLatestAsync(user.DeviceId, ct) is { Status: CashSessionStatus.EntregaPendiente } pending)
            throw new DomainException(
                $"Hay una entrega de caja pendiente: {pending.HandoverTo} debe confirmar Bs {pending.CountedAmount:0.00} (RF-062).");

        return await SaveOpenAsync(Open(openingAmount), ct);
    }

    private CashSession Open(decimal openingAmount)
    {
        var now = clock.Now;
        var session = CashSession.Open(Guid.NewGuid(), user.DeviceId, user.UserName, openingAmount, now);
        cash.Add(session);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashSessionOpened, session.Id,
            new CashSessionOpenedPayload(session.Id, session.DeviceId, session.CashierName, session.OpeningAmount,
                now), now));
        return session;
    }

    /// <summary>Confirma la apertura (separado de <see cref="Open"/> para abrir y confirmar entregas juntas).</summary>
    private async Task<CashSession> SaveOpenAsync(CashSession session, CancellationToken ct)
    {
        await unitOfWork.SaveChangesAsync(ct);
        return session;
    }

    /// <summary>
    /// SCRUM-126 · HU-16.2 (RF-061): el cajero que cerró el último turno entrega la caja al cajero entrante.
    /// La entrega queda pendiente hasta que este confirme el monto.
    /// </summary>
    public async Task<CashSession> RequestHandoverAsync(string incomingCashier, CancellationToken ct = default)
    {
        var session = await cash.GetLatestAsync(user.DeviceId, ct)
            ?? throw new DomainException("No hay un turno cerrado para entregar.");
        if (session.CashierName != user.UserName)
            throw new DomainException("Solo el cajero que cerró el turno puede entregar la caja.");

        session.RequestHandover(incomingCashier.Trim());
        var now = clock.Now;
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashHandoverRequested, session.Id,
            new CashHandoverPayload(session.Id, session.DeviceId, session.CashierName, session.HandoverTo,
                session.CountedAmount ?? 0, null, false, now), now, version: 3));
        await unitOfWork.SaveChangesAsync(ct);
        return session;
    }

    /// <summary>
    /// SCRUM-126 · HU-16.2 (RF-062): el cajero entrante confirma el monto recibido y su conformidad. Si coincide
    /// con lo contado al cierre, la entrega se completa y su turno se abre con ese monto; si no, la
    /// entrega sigue pendiente y la observación queda registrada.
    /// </summary>
    public async Task<bool> ConfirmHandoverAsync(decimal receivedAmount, bool agrees, CancellationToken ct = default)
    {
        var session = await cash.GetLatestAsync(user.DeviceId, ct);
        if (session is not { Status: CashSessionStatus.EntregaPendiente })
            throw new DomainException("No hay una entrega de caja pendiente en este puesto.");

        var now = clock.Now;
        session.ConfirmHandover(user.UserName, receivedAmount, agrees, now);
        var confirmed = session.Status == CashSessionStatus.Entregada;
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashHandoverConfirmed, session.Id,
            new CashHandoverPayload(session.Id, session.DeviceId, session.CashierName, session.HandoverTo,
                session.CountedAmount ?? 0, session.HandoverReceivedAmount, confirmed, now), now, version: 4));
        if (confirmed) Open(session.HandoverReceivedAmount!.Value);
        await unitOfWork.SaveChangesAsync(ct);
        return confirmed;
    }

    /// <summary>RF-055.</summary>
    public async Task<CashMovement> RegisterExpenseAsync(decimal amount, string reason, string category,
        CancellationToken ct = default)
    {
        var session = await RequireOpenAsync(ct);
        var now = clock.Now;
        var movement = session.RegisterExpense(amount, reason, category, user.UserName, now);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashMovementRegistered,
            movement.Id, new CashMovementPayload(movement.Id, session.Id, movement.Type.ToString(), movement.Amount,
                movement.Reason, movement.Category, movement.UserName, now), now));
        await unitOfWork.SaveChangesAsync(ct);
        return movement;
    }

    /// <summary>RF-057 a RF-060 y RF-106.</summary>
    public async Task<CashSessionClosedPayload> CloseAsync(IReadOnlyDictionary<decimal, int> denominations,
        string? justification, CancellationToken ct = default)
    {
        var session = await RequireOpenAsync(ct);
        var now = clock.Now;
        session.Close(new CashCount(denominations), justification, now);

        var report = new CashSessionClosedPayload(session.Id, session.DeviceId, session.CashierName,
            session.OpeningAmount, session.SalesTotal, session.ExpensesTotal, session.ExpectedAmount,
            session.CountedAmount!.Value, session.Difference!.Value, session.Justification, session.OpenedAt, now,
            denominations.Where(d => d.Value > 0).ToDictionary());
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashSessionClosed, session.Id,
            report, now, version: 2));
        await unitOfWork.SaveChangesAsync(ct);
        return report;
    }

    private async Task<CashSession> RequireOpenAsync(CancellationToken ct) =>
        await cash.GetOpenAsync(user.DeviceId, ct) ?? throw new DomainException("No hay una caja abierta.");
}
