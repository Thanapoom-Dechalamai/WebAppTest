using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Data.Support;

/// <summary>
/// Read side of the ticket page (/Staff/Cases): the four list counts, one list (filtered, searched, newest
/// activity first) and the selected ticket with its right-hand panel. Reads only; every change goes through
/// <see cref="SupportService"/> or <see cref="SupportTicketService"/>. Filters and numbers come from
/// <see cref="SupportTicketRules"/>, so what is unit tested is what runs here.
/// </summary>
public sealed class SupportInboxQuery(ApplicationDbContext db, SupportService support)
{
    public const int PageSize = 100;
    private const int OtherTicketsShown = 10;
    private const int HistoryShown = 20;

    /// <summary>Null when <paramref name="selectedId"/> names a ticket that does not exist (shown as 404).</summary>
    public async Task<SupportInboxPage?> LoadAsync(int actorId, string? view, string? query, int? selectedId)
    {
        var cases = db.SupportCases.AsNoTracking();
        SupportCase? selected = null;
        if (selectedId is int id && (selected = await cases.SingleOrDefaultAsync(x => x.Id == id)) is null) return null;

        var counts = new Dictionary<string, int>();
        foreach (var name in SupportInboxView.All)
            counts[name] = await cases.CountAsync(SupportTicketRules.InView(name, actorId));
        var current = SupportInboxView.Parse(view)
            ?? (selected is null
                ? SupportTicketRules.StartView(counts[SupportInboxView.Unassigned], counts[SupportInboxView.Mine])
                : SupportTicketRules.ViewOf(selected, actorId));

        var search = SupportTicketRules.ParseSearch(query);
        var list = cases.Where(SupportTicketRules.InView(current, actorId));
        if (search is not null)
        {
            // 0 when the search is not a number. Never null: EF turns "OrderId == null" into "OrderId IS NULL",
            // which would match every ticket without a booking. Ids start at 1, so 0 matches nothing.
            var (text, ticketId, orderId) = (search.Text, search.TicketId ?? 0, search.OrderId ?? 0);
            var customers = db.Users.Where(u => (u.FirstName + " " + u.LastName).Contains(text) || u.Email.Contains(text)).Select(u => u.Id);
            list = list.Where(c => c.Subject.Contains(text) || customers.Contains(c.CustomerId) || c.Id == ticketId || c.OrderId == orderId);
        }
        var rows = await RowsAsync(await list
            .OrderBy(x => x.Status == SupportStatus.Open ? 0 : x.Status == SupportStatus.Answered ? 1 : 2)
            .ThenByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id)
            .Take(PageSize).ToListAsync());

        var views = SupportInboxView.All.Select(x => new SupportViewCount(x, SupportInboxView.Label(x), counts[x])).ToList();
        var detail = selected is null ? null : await DetailAsync(selected);
        return new SupportInboxPage(actorId, current, search?.Text, views, rows, detail);
    }

    private async Task<SupportTicketDetail?> DetailAsync(SupportCase ticket)
    {
        var thread = await support.GetAsync(ticket.Id, null);
        if (thread is null) return null;
        var owners = await db.Users.AsNoTracking()
            .Where(x => (x.Role == AppRoles.Staff || x.Role == AppRoles.Admin) && !x.IsSuspended)
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(x => new SupportOwnerOption(x.Id, x.FirstName + " " + x.LastName, x.Role)).ToListAsync();
        var assignee = ticket.AssigneeUserId is int ownerId
            ? await db.Users.AsNoTracking().Where(x => x.Id == ownerId)
                .Select(x => new SupportOwner(x.Id, x.FirstName + " " + x.LastName, x.Role, x.IsSuspended)).SingleOrDefaultAsync()
            : null;
        var others = await RowsAsync(await db.SupportCases.AsNoTracking()
            .Where(x => x.CustomerId == ticket.CustomerId && x.Id != ticket.Id)
            .OrderByDescending(x => x.UpdatedAtUtc).Take(OtherTicketsShown).ToListAsync());
        var history = await db.SupportAssignments.AsNoTracking().Where(x => x.CaseId == ticket.Id)
            .OrderByDescending(x => x.Id).Take(HistoryShown).ToListAsync();
        var names = await NamesAsync(history.SelectMany(x => new[] { x.ActorUserId, x.FromUserId, x.ToUserId }));
        string? Name(int? userId) => userId is int u ? names.GetValueOrDefault(u)?.Name ?? "ผู้ใช้ที่ถูกลบ" : null;
        var historyRows = history.Select(x => new SupportAssignmentView(x, Name(x.ActorUserId), Name(x.FromUserId), Name(x.ToUserId))).ToList();
        return new SupportTicketDetail(thread, SupportTicketRules.Number(ticket.Id), assignee, owners, others, historyRows);
    }

    // 'cases' arrives filtered, sorted and limited. Names, latest message and unread counts come in three small queries.
    private async Task<IReadOnlyList<SupportTicketRow>> RowsAsync(List<SupportCase> cases)
    {
        if (cases.Count == 0) return [];
        var ids = cases.Select(x => x.Id).ToList();
        var people = await NamesAsync(cases.Select(x => (int?)x.CustomerId).Concat(cases.Select(x => x.AssigneeUserId)));
        var flags = await db.SupportMessages.AsNoTracking().Where(x => ids.Contains(x.CaseId)).OrderBy(x => x.Id)
            .Select(x => new { x.CaseId, x.Id, x.FromStaff }).ToListAsync();
        var threads = flags.GroupBy(x => x.CaseId).ToDictionary(g => g.Key, g => g.ToList());
        var lastIds = threads.Values.Select(x => x[^1].Id).ToList();
        var latest = await db.SupportMessages.AsNoTracking().Where(x => lastIds.Contains(x.Id))
            .Select(x => new { x.CaseId, x.Body, HasFile = x.Attachment != null, x.CreatedAtUtc }).ToDictionaryAsync(x => x.CaseId);
        return cases.Select(c =>
        {
            var customer = people.GetValueOrDefault(c.CustomerId);
            var name = customer?.Name ?? "ผู้ใช้ที่ถูกลบ";
            var last = latest.GetValueOrDefault(c.Id);
            var unread = SupportTicketRules.UnreadCount(c.Status, threads.GetValueOrDefault(c.Id)?.Select(x => x.FromStaff) ?? []);
            var owner = c.AssigneeUserId is int a ? people.GetValueOrDefault(a)?.Name ?? "ผู้ใช้ที่ถูกลบ" : null;
            return new SupportTicketRow(c, SupportTicketRules.Number(c.Id), name, customer?.Email ?? "", SupportTicketRules.Initials(name),
                SupportTicketRules.Preview(last?.Body, last?.HasFile == true), last?.CreatedAtUtc ?? c.UpdatedAtUtc, unread, owner);
        }).ToList();
    }

    private sealed record Person(string Name, string Email);

    private async Task<Dictionary<int, Person>> NamesAsync(IEnumerable<int?> userIds)
    {
        var ids = userIds.OfType<int>().Distinct().ToList();
        if (ids.Count == 0) return new();
        return await db.Users.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName, x.Email })
            .ToDictionaryAsync(x => x.Id, x => new Person(x.Name, x.Email));
    }
}
