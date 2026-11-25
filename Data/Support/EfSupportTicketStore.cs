using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services.Support;

namespace Nontiwat_7362_L002.Data.Support;

/// <summary>EF implementation of <see cref="ISupportTicketStore"/>. Shares the request's DbContext with the unit of work.</summary>
public sealed class EfSupportTicketStore(ApplicationDbContext db) : ISupportTicketStore
{
    public async Task<SupportCase?> LockCaseAsync(int caseId) =>
        (await db.SupportCases.FromSqlInterpolated($"SELECT * FROM support_cases WHERE Id = {caseId} FOR UPDATE").ToListAsync())
        .SingleOrDefault();

    public Task<SupportOwner?> FindOwnerAsync(int userId) =>
        db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new SupportOwner(x.Id, x.FirstName + " " + x.LastName, x.Role, x.IsSuspended))
            .SingleOrDefaultAsync();

    public async Task<IReadOnlyList<SupportCase>> LockOpenTicketsOwnedByAsync(int ownerId) =>
        await db.SupportCases.FromSqlInterpolated(
            $"SELECT * FROM support_cases WHERE AssigneeUserId = {ownerId} AND Status <> {SupportStatus.Closed} ORDER BY Id FOR UPDATE").ToListAsync();

    public void AddAssignment(SupportAssignment row) => db.SupportAssignments.Add(row);

    public void Notify(int userId, string title, string body, string url) =>
        db.Notifications.Add(new Notification { UserId = userId, OrderId = null, Title = title, Body = body, Url = url });
}
