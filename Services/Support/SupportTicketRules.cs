using System.Globalization;
using System.Linq.Expressions;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;

namespace Nontiwat_7362_L002.Services.Support;

// The four lists on the left of the ticket page. Values are the ?view= query string.
public static class SupportInboxView
{
    public const string Unassigned = "unassigned"; // open, nobody owns it
    public const string Mine = "mine";             // open, owned by the person looking
    public const string Open = "open";             // every ticket that is not closed
    public const string Closed = "closed";
    public static readonly string[] All = [Unassigned, Mine, Open, Closed];

    public static string Label(string view) => view switch
    {
        Unassigned => "ยังไม่มีคนรับ",
        Mine => "ของฉัน",
        Open => "เปิดอยู่ทั้งหมด",
        Closed => "ปิดแล้ว",
        _ => view
    };

    // null for anything that is not one of the four (the page then picks a start view).
    public static string? Parse(string? view) => All.FirstOrDefault(x => string.Equals(x, view?.Trim(), StringComparison.OrdinalIgnoreCase));

    // Old links (?status=Open from the today panel, bookmarks) still land on a sensible list.
    public static string? FromStatus(string? status) => status switch
    {
        SupportStatus.Closed => Closed,
        SupportStatus.Open or SupportStatus.Answered => Open,
        _ => null
    };
}

// A change of owner decided by SupportTicketRules; the service writes it to the case and to support_assignments.
public sealed record AssignmentChange(int? From, int? To, string Kind);

// What the search box understood: free text, plus the ticket or booking number it may be.
public sealed record SupportSearch(string Text, int? TicketId, int? OrderId);

// Who hears about a customer's reply on a ticket (owner's rule, 2026-11-25).
public enum ReplyAudience
{
    Assignee,        // the ticket has an owner who can still own it: only them
    Staff,           // nobody owns it: every Staff (not Admin)
    StaffAndAdmins   // the owner was suspended, moved out of Staff/Admin or deleted: everyone, so the reply is not lost
}

// Pure ticket rules: no database, no clock. Everything here is unit tested (SupportTicketRulesTests).
public static class SupportTicketRules
{
    public const string Prefix = "TK";
    public const int MaxSearch = 100;

    public static string Number(int id) => Prefix + id.ToString("D6", CultureInfo.InvariantCulture);

    // Staff and Admin who are not suspended can own a ticket (the same people NotifyStaffAsync tells).
    public static bool CanOwn(string? role, bool suspended) =>
        !suspended && (role == AppRoles.Staff || role == AppRoles.Admin);

    // Claim (to = actor), give to someone else, or release (to = null).
    // Returns null when nothing changes. Throws ArgumentException with a Thai message when it is not allowed.
    public static AssignmentChange? Assign(SupportCase ticket, int? to, int actorId, bool targetCanOwn)
    {
        if (ticket.Status == SupportStatus.Closed) throw new ArgumentException("ticket นี้ปิดแล้ว เปลี่ยนผู้รับผิดชอบไม่ได้");
        if (to is not null && !targetCanOwn) throw new ArgumentException("มอบได้เฉพาะพนักงาน Support หรือ Admin ที่ยังใช้งานอยู่");
        if (to == ticket.AssigneeUserId) return null;
        var kind = to is null ? SupportAssignmentKind.Release : to == actorId ? SupportAssignmentKind.Claim : SupportAssignmentKind.Assign;
        return new AssignmentChange(ticket.AssigneeUserId, to, kind);
    }

    // The first support reply on a ticket nobody owns makes the person replying its owner.
    // Must be decided on the row locked by the reply (FOR UPDATE) so two replies cannot both take it.
    public static AssignmentChange? AutoClaim(SupportCase ticket, int actorId) =>
        ticket.AssigneeUserId is null && ticket.Status != SupportStatus.Closed
            ? new AssignmentChange(null, actorId, SupportAssignmentKind.AutoClaim)
            : null;

    // assigneeId is the ticket's AssigneeUserId; assignee is that account as it is now (null if it no longer exists).
    public static ReplyAudience ReplyRecipients(int? assigneeId, SupportOwner? assignee) =>
        assigneeId is null ? ReplyAudience.Staff
        : assignee is { CanOwn: true } && assignee.Id == assigneeId ? ReplyAudience.Assignee
        : ReplyAudience.StaffAndAdmins;

    public static SupportAssignment Record(int caseId, int actorId, AssignmentChange change, DateTime nowUtc) => new()
    {
        CaseId = caseId, ActorUserId = actorId, FromUserId = change.From, ToUserId = change.To,
        Kind = change.Kind, CreatedAtUtc = nowUtc
    };

    // Customer messages after the last support message (all of them when support has not answered yet).
    // A closed ticket needs nothing, so it shows 0. 'fromStaff' is the thread in order, oldest first.
    public static int UnreadCount(string status, IEnumerable<bool> fromStaff)
    {
        if (status == SupportStatus.Closed) return 0;
        var unread = 0;
        foreach (var staff in fromStaff) unread = staff ? 0 : unread + 1;
        return unread;
    }

    public const int PreviewLength = 80;

    // One line of the latest message for the list: spaces and line breaks squeezed, cut to PreviewLength.
    public static string Preview(string? body, bool hasAttachment)
    {
        var text = string.Join(' ', (body ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length == 0) return hasAttachment ? "แนบไฟล์" : "";
        return text.Length <= PreviewLength ? text : text[..(PreviewLength - 1)] + "…";
    }

    // Up to two letters for the round badge: the first letter of the first two words (lodgo has no profile photos).
    // Thai words that start with a leading vowel (เ แ โ ใ ไ) use the consonant after it, e.g. "ใจดี" → "จ".
    public static string Initials(string? name)
    {
        var words = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var letters = words.Take(2).Select(FirstLetter).Where(x => x.Length > 0).ToList();
        return letters.Count == 0 ? "?" : string.Concat(letters).ToUpperInvariant();
    }

    private static string FirstLetter(string word)
    {
        var start = 0;
        while (start < word.Length - 1 && word[start] is >= 'เ' and <= 'ไ') start++;
        // The base letter only: a tone mark or vowel above it would crowd the badge.
        return System.Text.Rune.GetRuneAt(word, start).ToString();
    }

    // Trims, limits to MaxSearch and reads TK000123 / LG000042 / 123 as ticket and booking numbers.
    // Returns null for an empty search. Numbers too big for int are left as plain text.
    public static SupportSearch? ParseSearch(string? query)
    {
        var text = query?.Trim() ?? "";
        if (text.Length == 0) return null;
        if (text.Length > MaxSearch) text = text[..MaxSearch];
        var ticket = NumberAfter(text, Prefix) ?? NumberAfter(text, "#");
        var order = NumberAfter(text, "LG");
        if (ticket is null && order is null && PositiveInt(text) is int plain) (ticket, order) = (plain, plain);
        return new SupportSearch(text, ticket, order);
    }

    private static int? NumberAfter(string text, string prefix) =>
        text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? PositiveInt(text[prefix.Length..]) : null;

    private static int? PositiveInt(string digits) =>
        digits.Length > 0 && digits.All(char.IsAsciiDigit) && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : null;

    // The filter for one list, as an expression so EF turns it into SQL.
    public static Expression<Func<SupportCase, bool>> InView(string view, int actorId) => view switch
    {
        SupportInboxView.Unassigned => c => c.Status != SupportStatus.Closed && c.AssigneeUserId == null,
        SupportInboxView.Mine => c => c.Status != SupportStatus.Closed && c.AssigneeUserId == actorId,
        SupportInboxView.Closed => c => c.Status == SupportStatus.Closed,
        _ => c => c.Status != SupportStatus.Closed
    };

    // The list a ticket appears in for this person, so a link to one ticket opens with it in the list.
    public static string ViewOf(SupportCase ticket, int actorId) =>
        ticket.Status == SupportStatus.Closed ? SupportInboxView.Closed
        : ticket.AssigneeUserId is null ? SupportInboxView.Unassigned
        : ticket.AssigneeUserId == actorId ? SupportInboxView.Mine
        : SupportInboxView.Open;

    // With no ?view=: tickets nobody has taken first, then your own, then everything open.
    public static string StartView(int unassigned, int mine) =>
        unassigned > 0 ? SupportInboxView.Unassigned : mine > 0 ? SupportInboxView.Mine : SupportInboxView.Open;
}
