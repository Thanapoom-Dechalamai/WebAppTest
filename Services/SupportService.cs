using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

// Support tickets: a customer opens a case (optionally about one booking), Lodgo support answers.
// Open → Answered (support replied) → Closed; a customer reply sends it back to Open. Every Staff/Admin sees
// every ticket; who owns one is handled by SupportTicketService (the first support reply takes an unowned ticket).
public class SupportService(ApplicationDbContext db, IWebHostEnvironment environment, SupportTicketService tickets) : ISupportChat
{
    // A quote line in the chat goes through the normal reply path: status, auto-claim and notifications all apply.
    public Task PostAsync(int actorId, int caseId, string body, bool fromStaff) => ReplyAsync(actorId, caseId, body, fromStaff);

    public const int MaxBody = 2000;
    private const string AttachmentFolder = "SupportAttachments";

    public async Task<int> OpenAsync(int customerId, NewCaseForm form)
    {
        if (form.OrderId is int orderId && !await db.Orders.AnyAsync(x => x.Id == orderId && x.UserId == customerId))
            throw new ArgumentException("ไม่พบการจองนี้ในบัญชีของคุณ");
        var body = MessageBody(form.Body, form.Attachment is not null);
        var now = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        var item = new SupportCase { CustomerId = customerId, OrderId = form.OrderId,
            HotelId = form.HotelId, RoomType = form.RoomType, CheckIn = form.CheckIn?.Date, CheckOut = form.CheckOut?.Date,
            Rooms = form.Rooms, Guests = form.Guests, PromotionCode = form.PromotionCode, SpecialRequest = form.SpecialRequest,
            Topic = SupportTopic.All.Contains(form.Topic) ? form.Topic : SupportTopic.Other,
            Subject = form.Subject.Trim(), Status = SupportStatus.Open, CreatedAtUtc = now, UpdatedAtUtc = now };
        db.SupportCases.Add(item);
        await db.SaveChangesAsync();
        await NotifyStaffAsync(item, "ลูกค้าแจ้งปัญหาใหม่");
        await AppendAsync(item.Id, customerId, body, false, now, form.Attachment);
        await tx.CommitAsync();
        return item.Id;
    }

    // Customer reply → Open, support reply → Answered. A closed case takes no more messages.
    public async Task ReplyAsync(int actorId, int caseId, string? body, bool fromStaff, IFormFile? attachment = null)
    {
        body = MessageBody(body, attachment is not null);
        await using var tx = await db.Database.BeginTransactionAsync();
        var item = await LoadAsync(actorId, caseId, fromStaff);
        if (item.Status == SupportStatus.Closed) throw new ArgumentException("เรื่องนี้ปิดแล้ว ถ้ายังมีปัญหาให้แจ้งเรื่องใหม่");
        var now = DateTime.UtcNow;
        if (fromStaff) tickets.AutoClaim(item, actorId);
        item.Status = fromStaff ? SupportStatus.Answered : SupportStatus.Open;
        item.UpdatedAtUtc = now;
        if (fromStaff) NotifyCustomer(item, "Lodgo support ตอบเรื่องที่คุณแจ้งแล้ว");
        else await NotifyCustomerReplyAsync(item);
        await AppendAsync(item.Id, actorId, body, fromStaff, now, attachment);
        await tx.CommitAsync();
    }

    public static string MessageBody(string? body, bool hasAttachment)
    {
        body = body?.Trim() ?? "";
        if (body.Length == 0 && !hasAttachment) throw new ArgumentException("กรอกข้อความหรือแนบไฟล์ก่อนส่ง");
        if (body.Length > MaxBody) throw new ArgumentException("ข้อความยาวได้ไม่เกิน 2,000 ตัวอักษร");
        return body;
    }

    private async Task AppendAsync(int caseId, int actorId, string body, bool fromStaff, DateTime now, IFormFile? attachment)
    {
        string? name = null;
        if (attachment is not null)
        {
            var saved = await UploadedFiles.SaveAsync(attachment, environment, AttachmentFolder);
            if (saved.Error is not null) throw new ArgumentException(saved.Error);
            name = saved.Name;
        }
        db.SupportMessages.Add(new SupportMessage
        {
            CaseId = caseId, AuthorUserId = actorId, Body = body, FromStaff = fromStaff,
            CreatedAtUtc = now, Attachment = name
        });
        try { await db.SaveChangesAsync(); }
        catch
        {
            var path = UploadedFiles.PathOf(environment, AttachmentFolder, name);
            if (path is not null) File.Delete(path);
            throw;
        }
    }

    // OnRecord: the message exists, may be seen by this person and has an attachment; Path is null when its file is gone.
    public async Task<(bool OnRecord, string? Path)> AttachmentAsync(int messageId, int? customerId)
    {
        var file = await (from message in db.SupportMessages.AsNoTracking()
                          join item in db.SupportCases.AsNoTracking() on message.CaseId equals item.Id
                          where message.Id == messageId && (customerId == null || item.CustomerId == customerId)
                          select message.Attachment).SingleOrDefaultAsync();
        return (file is not null, UploadedFiles.PathOf(environment, AttachmentFolder, file));
    }

    public async Task CloseAsync(int actorId, int caseId, bool asStaff)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var item = await LoadAsync(actorId, caseId, asStaff);
        if (item.Status == SupportStatus.Closed) return;
        item.Status = SupportStatus.Closed;
        item.UpdatedAtUtc = DateTime.UtcNow;
        if (asStaff) NotifyCustomer(item, "Lodgo support ปิดเรื่องที่คุณแจ้งแล้ว");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public Task<IReadOnlyList<SupportCaseRow>> ForCustomerAsync(int customerId) =>
        RowsAsync(db.SupportCases.AsNoTracking().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.UpdatedAtUtc));

    // The staff ticket list is Data/Support/SupportInboxQuery (since 2026-11-25).

    // customerId: only that customer's case (else null). null: any case (support).
    public async Task<SupportCaseView?> GetAsync(int caseId, int? customerId)
    {
        var item = await db.SupportCases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == caseId && (customerId == null || x.CustomerId == customerId));
        if (item is null) return null;
        var customer = await db.Users.AsNoTracking().Where(x => x.Id == item.CustomerId).Select(x => new { Name = x.FirstName + " " + x.LastName, x.Email }).SingleAsync();
        var messages = await (from m in db.SupportMessages.AsNoTracking() join u in db.Users.AsNoTracking() on m.AuthorUserId equals u.Id into authors
                              from u in authors.DefaultIfEmpty()
                              where m.CaseId == caseId orderby m.Id
                              select new SupportMessageView(m, u == null ? "ผู้ใช้ที่ถูกลบ" : u.FirstName + " " + u.LastName)).ToListAsync();
        var quotes = await db.SupportQuotes.AsNoTracking().Where(x => x.CaseId == caseId).OrderByDescending(x => x.Id).ToListAsync();
        return new SupportCaseView(item, customer.Name, customer.Email, await OrderInfoAsync(item.OrderId, null), messages) { Quotes = quotes };
    }

    public async Task<SupportOrderInfo?> OrderInfoAsync(int? orderId, int? customerId) => orderId is null ? null :
        await db.Orders.AsNoTracking().Where(x => x.Id == orderId && (customerId == null || x.UserId == customerId))
            .Select(x => new SupportOrderInfo(x.Id, x.HotelName, x.CheckIn, x.CheckOut, x.Status, x.Total)).SingleOrDefaultAsync();

    public Task<bool> OrderHasSlipAsync(int orderId, string slip) => db.Orders.AnyAsync(x => x.Id == orderId && x.SlipFile == slip);

    public Task<int> OpenCountAsync() => db.SupportCases.CountAsync(x => x.Status == SupportStatus.Open);

    // Cases support has answered that the customer has not read/replied to yet.
    public Task<int> WaitingCountAsync(int customerId) =>
        db.SupportCases.CountAsync(x => x.CustomerId == customerId && x.Status == SupportStatus.Answered);

    // 'cases' arrives already filtered, sorted and limited; names and message counts are added in two small queries.
    private async Task<IReadOnlyList<SupportCaseRow>> RowsAsync(IQueryable<SupportCase> cases)
    {
        var list = await cases.ToListAsync();
        var ids = list.Select(x => x.Id).ToList();
        var customerIds = list.Select(x => x.CustomerId).Distinct().ToList();
        var people = await db.Users.AsNoTracking().Where(x => customerIds.Contains(x.Id))
            .Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName, x.Email }).ToDictionaryAsync(x => x.Id);
        var counts = await db.SupportMessages.AsNoTracking().Where(x => ids.Contains(x.CaseId))
            .GroupBy(x => x.CaseId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return list.Select(c => new SupportCaseRow(c, people.TryGetValue(c.CustomerId, out var p) ? p.Name : "ผู้ใช้ที่ถูกลบ",
            people.TryGetValue(c.CustomerId, out var q) ? q.Email : "", counts.GetValueOrDefault(c.Id))).ToList();
    }

    // Throws KeyNotFoundException when a customer asks for someone else's case (shown as 404).
    // Called inside a transaction: FOR UPDATE makes a close and a reply that arrive together run one after the
    // other, so a reply can never reopen a case that was just closed.
    private async Task<SupportCase> LoadAsync(int actorId, int caseId, bool asStaff) =>
        (await db.SupportCases.FromSqlInterpolated($"SELECT * FROM support_cases WHERE Id = {caseId} FOR UPDATE").ToListAsync())
        .SingleOrDefault(x => asStaff || x.CustomerId == actorId)
        ?? throw new KeyNotFoundException();

    private void NotifyCustomer(SupportCase item, string title) =>
        db.Notifications.Add(new Notification { UserId = item.CustomerId, OrderId = null, Title = title,
            Body = Cut($"เรื่อง: {item.Subject}", 255), Url = $"/Support/Case/{item.Id}" });

    // A new ticket: every active Staff and Admin (nobody owns it yet).
    private async Task NotifyStaffAsync(SupportCase item, string title, bool includeAdmins = true)
    {
        var staff = await db.Users.AsNoTracking()
            .Where(x => (x.Role == AppRoles.Staff || includeAdmins && x.Role == AppRoles.Admin) && !x.IsSuspended)
            .Select(x => x.Id).ToListAsync();
        foreach (var id in staff) NotifyTeamMember(id, item, title);
    }

    // A customer reply: the owner only; every Staff when nobody owns it; everyone when the owner can no longer own it
    // (SupportTicketRules.ReplyRecipients). Runs on the row ReplyAsync locked, so the owner read here is the current one.
    private async Task NotifyCustomerReplyAsync(SupportCase item)
    {
        const string title = "ลูกค้าตอบกลับเรื่องที่แจ้ง";
        var owner = item.AssigneeUserId is int ownerId
            ? await db.Users.AsNoTracking().Where(x => x.Id == ownerId)
                .Select(x => new SupportOwner(x.Id, x.FirstName + " " + x.LastName, x.Role, x.IsSuspended)).SingleOrDefaultAsync()
            : null;
        switch (SupportTicketRules.ReplyRecipients(item.AssigneeUserId, owner))
        {
            case ReplyAudience.Assignee: NotifyTeamMember(owner!.Id, item, title); break;
            case ReplyAudience.Staff: await NotifyStaffAsync(item, title, includeAdmins: false); break;
            default: await NotifyStaffAsync(item, title); break;
        }
    }

    private void NotifyTeamMember(int userId, SupportCase item, string title) =>
        db.Notifications.Add(new Notification { UserId = userId, OrderId = null, Title = title,
            Body = Cut($"{SupportTicketRules.Number(item.Id)} · {item.Subject}", 255), Url = SupportTicketService.StaffUrl(item.Id) });

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
