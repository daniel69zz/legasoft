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

    /// <summary>RF-052.</summary>
    public async Task<CashSession> OpenAsync(decimal openingAmount, CancellationToken ct = default)
    {
        if (await cash.GetOpenAsync(user.DeviceId, ct) is not null)
            throw new DomainException("Ya hay una caja abierta en este puesto.");

        var now = clock.Now;
        var session = CashSession.Open(Guid.NewGuid(), user.DeviceId, user.UserName, openingAmount, now);
        cash.Add(session);
        outbox.Add(OutboxMessage.Create(user.DeviceId, SyncModules.Cash, EventTypes.CashSessionOpened, session.Id,
            new CashSessionOpenedPayload(session.Id, session.DeviceId, session.CashierName, session.OpeningAmount,
                now), now));
        await unitOfWork.SaveChangesAsync(ct);
        return session;
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
