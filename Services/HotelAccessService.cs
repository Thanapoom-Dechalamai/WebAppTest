using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
namespace Nontiwat_7362_L002.Services;

// Hotel staff ask to look after a hotel and attach proof that they work there; Admin or Manager decides.
// Approving adds the account to the hotel's team (hotel_assignments), the same as Admin assigning it by hand.
public class HotelAccessService(ApplicationDbContext db)
{
    public record Row(HotelAccessRequest Request, string HotelName, string City, string Email, string Name, string? ReviewerEmail);

    public async Task SubmitAsync(int userId, string hotelId, string? message, string proofFile)
    {
        var role = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.Role).SingleOrDefaultAsync();
        if (role != AppRoles.HotelOwner) throw new ArgumentException("ส่งคำขอได้เฉพาะบัญชีพนักงานโรงแรม");
        if (!await db.Hotels.AnyAsync(x => x.Id == hotelId && x.IsActive)) throw new ArgumentException("ไม่พบโรงแรม");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // The user row lock makes two quick submits for the same hotel queue up, so the duplicate check below holds.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM users WHERE Id = {userId} FOR UPDATE");
        if (await db.HotelAssignments.AnyAsync(x => x.OwnerUserId == userId && x.HotelId == hotelId))
            throw new ArgumentException("คุณดูแลโรงแรมนี้อยู่แล้ว");
        if (await db.HotelAccessRequests.AnyAsync(x => x.UserId == userId && x.HotelId == hotelId && x.Status == AccessRequestStatus.Pending))
            throw new ArgumentException("มีคำขอของโรงแรมนี้รอพิจารณาอยู่แล้ว");
        message = message?.Trim();
        db.HotelAccessRequests.Add(new HotelAccessRequest { UserId = userId, HotelId = hotelId, ProofFile = proofFile,
            Message = string.IsNullOrEmpty(message) ? null : message.Length > 500 ? message[..500] : message });
        var reviewers = await db.Users.AsNoTracking().Where(x => (x.Role == AppRoles.Admin || x.Role == AppRoles.Manager) && !x.IsSuspended)
            .Select(x => x.Id).ToListAsync();
        var hotel = await db.Hotels.AsNoTracking().Where(x => x.Id == hotelId).Select(x => x.Name).SingleAsync();
        foreach (var id in reviewers)
            db.Notifications.Add(new Notification { UserId = id, Title = "คำขอสิทธิ์ดูแลโรงแรม", Body = Cut("รอพิจารณา · " + hotel, 255), Url = "/HotelRequests" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public Task<List<Row>> QueueAsync() => RowsAsync(db.HotelAccessRequests.AsNoTracking(), 200);
    public Task<List<Row>> MineAsync(int userId) => RowsAsync(db.HotelAccessRequests.AsNoTracking().Where(x => x.UserId == userId), 50);
    public async Task<Row?> FindAsync(int id) => (await RowsAsync(db.HotelAccessRequests.AsNoTracking().Where(x => x.Id == id), 1)).FirstOrDefault();
    public Task<int> PendingCountAsync() => db.HotelAccessRequests.CountAsync(x => x.Status == AccessRequestStatus.Pending);

    private async Task<List<Row>> RowsAsync(IQueryable<HotelAccessRequest> q, int take) =>
        await (from r in q
               join h in db.Hotels.AsNoTracking() on r.HotelId equals h.Id
               join u in db.Users.AsNoTracking() on r.UserId equals u.Id
               join v in db.Users.AsNoTracking() on r.ReviewerId equals v.Id into rv
               from v in rv.DefaultIfEmpty()
               orderby r.Status == AccessRequestStatus.Pending descending, r.CreatedAtUtc descending
               select new Row(r, h.Name, h.City, u.Email, u.FirstName + " " + u.LastName, v == null ? null : v.Email)).Take(take).ToListAsync();

    public async Task ApproveAsync(int actorId, int id) => await DecideAsync(actorId, id, true, null);
    public async Task RejectAsync(int actorId, int id, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 300) throw new ArgumentException("ระบุเหตุผลที่ไม่อนุมัติ 1–300 ตัวอักษร");
        await DecideAsync(actorId, id, false, reason.Trim());
    }

    private async Task DecideAsync(int actorId, int id, bool approve, string? reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Lock the request itself first: two reviewers deciding at once queue up, and the second sees the first's outcome.
        var request = (await db.HotelAccessRequests.FromSqlInterpolated($"SELECT * FROM hotel_access_requests WHERE Id = {id} FOR UPDATE").ToListAsync()).SingleOrDefault()
            ?? throw new ArgumentException("ไม่พบคำขอ");
        if (request.Status != AccessRequestStatus.Pending) throw new ArgumentException($"คำขอนี้{AccessRequestStatus.Label(request.Status)}ไปแล้ว");
        // The user row lock serialises this with role changes and deletion (as in HotelOwnershipService).
        var staff = (await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE Id = {request.UserId} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (approve && staff?.Role != AppRoles.HotelOwner) throw new ArgumentException("บัญชีนี้ไม่ได้เป็นพนักงานโรงแรมแล้ว");
        var hotel = await db.Hotels.AsNoTracking().Where(x => x.Id == request.HotelId).Select(x => x.Name).SingleAsync();
        if (approve && !await db.HotelAssignments.AnyAsync(x => x.HotelId == request.HotelId && x.OwnerUserId == request.UserId))
            db.HotelAssignments.Add(new HotelAssignment { HotelId = request.HotelId, OwnerUserId = request.UserId, UpdatedAtUtc = DateTime.UtcNow });
        request.Status = approve ? AccessRequestStatus.Approved : AccessRequestStatus.Rejected;
        request.ReviewerId = actorId; request.ReviewNote = reason; request.ReviewedAtUtc = DateTime.UtcNow;
        db.Notifications.Add(new Notification { UserId = request.UserId, Url = approve ? "/HotelOwner/Index" : "/HotelOwner/Access",
            Title = approve ? "อนุมัติสิทธิ์ดูแลโรงแรมแล้ว" : "คำขอสิทธิ์ดูแลโรงแรมไม่ผ่าน",
            Body = Cut(approve ? $"{hotel} · เห็นและจัดการการจองของโรงแรมนี้ได้แล้ว" : $"{hotel} · {reason}", 255) });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static string Cut(string text, int max) => text.Length > max ? text[..max] : text;
}
