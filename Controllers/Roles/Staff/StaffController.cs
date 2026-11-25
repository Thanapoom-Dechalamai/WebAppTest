using System.Security.Claims;
using Nontiwat_7362_L002.Services.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.Data.Support;
using Nontiwat_7362_L002.ViewModels.Roles;

namespace Nontiwat_7362_L002.Controllers;

// Lodgo support handles customer conversations and follows up on bookings.
[Authorize(Policy = RolePolicies.Staff)]
public class StaffController(ApplicationDbContext db, CancellationService cancellation) : Controller
{
    public static readonly TimeSpan FollowUpAfter = TimeSpan.FromHours(24);

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        await cancellation.ExpireStalePendingAsync();
        search = search?.Trim();
        if (search?.Length > 100) search = search[..100];
        var staleBefore = DateTime.UtcNow - FollowUpAfter;
        var query = from order in db.Orders.AsNoTracking() join user in db.Users.AsNoTracking() on order.UserId equals user.Id
                    select new { Order = order, Name = user.FirstName + " " + user.LastName, user.Email };
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.Order.HotelName.Contains(search) || x.Email.Contains(search) || x.Name.Contains(search));
        if (status == "Stale") query = query.Where(x => x.Order.Status == BookingStatus.Pending && (x.Order.HotelSentAtUtc ?? x.Order.CreatedAtUtc) < staleBefore);
        else if (status is not null && BookingStatus.All.Contains(status)) query = query.Where(x => x.Order.Status == status);
        var rows = await query.OrderByDescending(x => x.Order.Id).Take(200)
            .Select(x => new StaffOrderRow(x.Order, x.Name, x.Email)).ToListAsync();
        var hotelIds = rows.Select(x => x.Order.HotelId).Distinct().ToList();
        var teams = (await (from a in db.HotelAssignments.AsNoTracking() join u in db.Users.AsNoTracking() on a.OwnerUserId equals u.Id
                            where hotelIds.Contains(a.HotelId) && u.Role == AppRoles.HotelOwner
                            select new { a.HotelId, u.FirstName, u.Email }).ToListAsync())
            .GroupBy(x => x.HotelId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => $"{x.FirstName} ({x.Email})")));
        var staleCount = await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending && (x.HotelSentAtUtc ?? x.CreatedAtUtc) < staleBefore);
        return View(new StaffDashboard(rows, search, status) { HotelTeams = teams, StaleCount = staleCount });
    }

    // ---------- Support tickets: one page (filters · list · chat · ticket panel) ----------
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // view = unassigned|mine|open|closed, q = search, id = the open ticket. 'status' keeps old links working.
    [HttpGet]
    public async Task<IActionResult> Cases(string? view, string? q, int? id, string? status, [FromServices] SupportInboxQuery inbox)
    {
        var page = await inbox.LoadAsync(ActorId, view ?? SupportInboxView.FromStatus(status), q, id);
        return page is null ? NotFound() : View(page);
    }

    // The old one-ticket page: links in earlier notifications still open the ticket.
    [HttpGet]
    public IActionResult Case(int id) => LocalRedirect(SupportTicketService.StaffUrl(id));

    // Back to the same list, search and ticket. Values are re-checked so only known ones go into the URL.
    private IActionResult BackToTicket(int id, string? view, string? q)
    {
        var url = SupportTicketService.StaffUrl(id);
        if (SupportInboxView.Parse(view) is string list) url += "&view=" + list;
        if (SupportTicketRules.ParseSearch(q)?.Text is string text) url += "&q=" + Uri.EscapeDataString(text);
        return LocalRedirect(url);
    }

    // to = a Staff/Admin id (yourself = take it), empty = leave it with nobody.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int? to, string? view, string? q, [FromServices] SupportTicketService tickets)
    {
        try
        {
            var change = await tickets.AssignAsync(ActorId, id, to);
            TempData["Success"] = change?.Kind switch
            {
                SupportAssignmentKind.Claim => "รับ ticket นี้แล้ว",
                SupportAssignmentKind.Assign => "มอบ ticket แล้ว ผู้รับได้รับแจ้งเตือน",
                SupportAssignmentKind.Release => "ปล่อย ticket แล้ว ตอนนี้ยังไม่มีคนรับ",
                _ => "ผู้รับผิดชอบเป็นคนเดิมอยู่แล้ว"
            };
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return BackToTicket(id, view, q);
    }

    [HttpGet]
    public async Task<IActionResult> Messages(int id, [FromServices] SupportService support)
    {
        var view = await support.GetAsync(id, null);
        ViewData["AsStaff"] = true;
        return view is null ? NotFound() : PartialView("~/Views/Support/_Messages.cshtml", view);
    }

    [HttpGet]
    public async Task<IActionResult> Attachment(int id, [FromServices] SupportService support)
    {
        var (onRecord, path) = await support.AttachmentAsync(id, null);
        if (path is null) return onRecord ? this.FileMissing() : NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, UploadedFiles.ContentType(path), "lodgo-attachment" + Path.GetExtension(path));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadedFiles.MaxBytes + 256 * 1024)]
    public async Task<IActionResult> Answer(int id, string? body, IFormFile? attachment, string? view, string? q, [FromServices] SupportService support)
    {
        try { await support.ReplyAsync(ActorId, id, body, fromStaff: true, attachment); TempData["Success"] = "ส่งคำตอบแล้ว ลูกค้าได้รับแจ้งเตือน"; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return BackToTicket(id, view, q);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, string? view, string? q, [FromServices] SupportService support)
    {
        try { await support.CloseAsync(ActorId, id, asStaff: true); TempData["Success"] = "ปิด ticket แล้ว"; }
        catch (KeyNotFoundException) { return NotFound(); }
        return BackToTicket(id, view, q);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(StaffCancellation form)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(form.Reason)) return BadRequest("ระบุรายการและเหตุผลไม่เกิน 120 ตัวอักษร");
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == form.Id);
        if (order is null) return NotFound();
        try
        {
            var actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await cancellation.SupportCancelAsync(actor, order.Id, order.UserId, $"Lodgo support #{actor}: {form.Reason.Trim()}");
            TempData["Success"] = "ยกเลิกคำขอ คืนเครดิตและคืนสิทธิ์โค้ดให้ลูกค้าแล้ว";
        }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
}
