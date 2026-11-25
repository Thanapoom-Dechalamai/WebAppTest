using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Support;

/// <summary>
/// Who owns a ticket: claim it, give it to another Staff/Admin, release it, and the automatic claim on the first
/// support reply. Rules live in <see cref="SupportTicketRules"/>; this class only runs them on a locked row and
/// stages the history row and the notification, so two people acting on one ticket at once cannot both win.
/// </summary>
public sealed class SupportTicketService(IUnitOfWork uow, ISupportTicketStore store, IThaiClock clock)
{
    public static string StaffUrl(int caseId) => $"/Staff/Cases?id={caseId}";

    /// <summary>
    /// Give the ticket to <paramref name="to"/> (the actor = claim, someone else = assign, null = release).
    /// Returns the change, or null when the ticket already had that owner. Throws <see cref="KeyNotFoundException"/>
    /// for a missing ticket and <see cref="ArgumentException"/> (Thai message) when the rules refuse.
    /// </summary>
    public async Task<AssignmentChange?> AssignAsync(int actorId, int caseId, int? to)
    {
        await uow.BeginAsync();
        try
        {
            var ticket = await store.LockCaseAsync(caseId) ?? throw new KeyNotFoundException();
            var target = to is int id ? await store.FindOwnerAsync(id) : null;
            var change = SupportTicketRules.Assign(ticket, to, actorId, target?.CanOwn == true);
            if (change is not null)
            {
                Apply(ticket, actorId, change);
                if (change.Kind == SupportAssignmentKind.Assign && change.To is int owner)
                    store.Notify(owner, "มีการมอบ ticket ให้คุณ",
                        Cut($"{SupportTicketRules.Number(ticket.Id)} · {ticket.Subject}", 255), StaffUrl(ticket.Id));
                await uow.SaveAsync();
            }
            await uow.CommitAsync();
            return change;
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    public Task<AssignmentChange?> ClaimAsync(int actorId, int caseId) => AssignAsync(actorId, caseId, actorId);

    public Task<AssignmentChange?> ReleaseAsync(int actorId, int caseId) => AssignAsync(actorId, caseId, null);

    /// <summary>
    /// For <c>SupportService.ReplyAsync</c>: call on the ticket it already locked, inside its transaction, before
    /// it saves. A support reply on a ticket nobody owns makes the person replying its owner.
    /// </summary>
    public AssignmentChange? AutoClaim(SupportCase lockedTicket, int actorId)
    {
        var change = SupportTicketRules.AutoClaim(lockedTicket, actorId);
        if (change is not null) Apply(lockedTicket, actorId, change);
        return change;
    }

    /// <summary>
    /// After an account is suspended or moved out of Staff/Admin (B5, 2026-11-25): its open tickets go back to
    /// "ยังไม่มีคนรับ", each with a history row saying why. Does nothing while the person can still own tickets.
    /// Returns how many tickets were released.
    /// </summary>
    public async Task<int> ReleaseIfCannotOwnAsync(int actorId, int ownerId)
    {
        var owner = await store.FindOwnerAsync(ownerId);
        if (owner is { CanOwn: true }) return 0;
        var kind = owner is { IsSuspended: true } ? SupportAssignmentKind.Suspended : SupportAssignmentKind.RoleChanged;
        await uow.BeginAsync();
        try
        {
            var owned = await store.LockOpenTicketsOwnedByAsync(ownerId);
            foreach (var ticket in owned) Apply(ticket, actorId, new AssignmentChange(ownerId, null, kind));
            if (owned.Count > 0) await uow.SaveAsync();
            await uow.CommitAsync();
            return owned.Count;
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
    }

    // Changing the owner does not touch UpdatedAtUtc: the list is ordered by conversation activity.
    private void Apply(SupportCase ticket, int actorId, AssignmentChange change)
    {
        ticket.AssigneeUserId = change.To;
        store.AddAssignment(SupportTicketRules.Record(ticket.Id, actorId, change, clock.UtcNow));
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
