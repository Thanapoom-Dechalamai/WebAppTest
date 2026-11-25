using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Support;

/// <summary>A person who may (or may no longer) own a ticket, as the assignment rules need them.</summary>
public sealed record SupportOwner(int Id, string Name, string? Role, bool IsSuspended)
{
    public bool CanOwn => SupportTicketRules.CanOwn(Role, IsSuspended);
}

/// <summary>
/// What the ticket workflow needs from the database. The EF implementation is
/// <c>Data/Support/EfSupportTicketStore</c>; unit tests replace it with NSubstitute. Add* and Notify only stage
/// rows: <see cref="IUnitOfWork.SaveAsync"/> writes them together with the ticket change.
/// </summary>
public interface ISupportTicketStore
{
    /// <summary>The ticket locked with <c>FOR UPDATE</c> (inside the open transaction), or null if there is none.</summary>
    Task<SupportCase?> LockCaseAsync(int caseId);

    /// <summary>A user by id with role and suspension, or null if the account does not exist.</summary>
    Task<SupportOwner?> FindOwnerAsync(int userId);

    /// <summary>Open (not closed) tickets owned by this person, locked with FOR UPDATE inside the open transaction.</summary>
    Task<IReadOnlyList<SupportCase>> LockOpenTicketsOwnedByAsync(int ownerId);

    /// <summary>Stage one row of assignment history.</summary>
    void AddAssignment(SupportAssignment row);

    /// <summary>Stage a bell notification for one user.</summary>
    void Notify(int userId, string title, string body, string url);
}
