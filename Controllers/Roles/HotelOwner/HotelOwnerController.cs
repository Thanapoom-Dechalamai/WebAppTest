using System.Security.Claims;
using Nontiwat_7362_L002.Services.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Controllers;

// Hotel staff workspace: a member sees only bookings of the hotels whose team they are on.
[Authorize(Policy=RolePolicies.HotelOwner)]
public class HotelOwnerController(ApplicationDbContext db, HotelDeskService hotelDesk, CancellationService cancellation, Nontiwat_7362_L002.Services.CatalogService catalog, AvailabilityService availability, IThaiClock clock) : Controller
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? range, DateTime? day, string? hotelId)
    {
        await cancellation.ExpireStalePendingAsync();
        var admin = User.IsInRole(AppRoles.Admin);
        var today = clock.Today;
        var hotelIds = await db.HotelAssignments.Where(x => x.OwnerUserId == ActorId).Select(x => x.HotelId).Distinct().ToListAsync();
        // A transfer Lodgo has not checked yet is not the hotel's business (it reaches the hotel as Pending once paid).
        var mine = db.Orders.AsNoTracking().Where(x => (admin || hotelIds.Contains(x.HotelId)) && x.Status != BookingStatus.PaymentReview);
        var pending = await mine.Where(x => x.Status == BookingStatus.Pending).SumAsync(x => (decimal?)x.Total) ?? 0;
        var queue = new HotelWorkQueue(
            await mine.CountAsync(x => x.Status == BookingStatus.Pending),
            await mine.CountAsync(x => x.Status == BookingStatus.Confirmed && x.CheckIn <= today && x.CheckOut > today),
            await mine.CountAsync(x => x.Status == BookingStatus.CheckedIn));
        var orders = mine;
        if (status == "Arriving") orders = orders.Where(x => x.Status == BookingStatus.Confirmed && x.CheckIn <= today && x.CheckOut > today);
        else if (status is not null && BookingStatus.All.Contains(status)) orders = orders.Where(x => x.Status == status);
        var rows = await (from order in orders join customer in db.Users.AsNoTracking() on order.UserId equals customer.Id
            join decision in db.HotelDecisions.AsNoTracking() on order.Id equals decision.OrderId into decisions
            from decision in decisions.DefaultIfEmpty()
            orderby order.Id descending
            select new HotelOwnerOrder(order, customer.FirstName+" "+customer.LastName, customer.Email, decision == null ? null : decision.Note)).Take(200).ToListAsync();
        // Revenue belongs to the hotel, so every team member sees the same figures.
        var entries = db.HotelOwnerEntries.AsNoTracking().Where(x => admin || hotelIds.Contains(x.HotelId));
        var earned = (await mine.Where(x => FinanceRules.EarnedStatuses.Contains(x.Status)).Select(x => new { x.Total, x.CommissionPercent }).ToListAsync())
            .Select(x => (x.Total, x.CommissionPercent)).ToList();
        var paidOut = await db.HotelPayouts.AsNoTracking().Where(x => admin || hotelIds.Contains(x.HotelId)).SumAsync(x => (decimal?)x.Amount) ?? 0;
        // Booking summary for the donut: bookings made in the chosen range, grouped the way the hotel thinks about them.
        range = range is "month" or "30" ? range : "week";
        var since = range switch { "month" => new DateTime(today.Year, today.Month, 1).AddHours(-7), "30" => DateTime.UtcNow.AddDays(-30), _ => DateTime.UtcNow.AddDays(-7) };
        var counts = await mine.Where(x => x.CreatedAtUtc >= since).GroupBy(x => x.Status).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        int Count(params string[] s) => counts.Where(x => s.Contains(x.Key)).Sum(x => x.N);
        var summary = new BookingSummary(range, Count(BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.Completed),
            Count(BookingStatus.Pending), Count(BookingStatus.Cancelled, BookingStatus.NoShow));
        // The day strip: one hotel at a time (the first one the user looks after, or the one picked).
        var hotels = (await catalog.AllAsync(includeInactive: true)).Where(h => admin || hotelIds.Contains(h.Id)).ToList();
        var shown = hotels.FirstOrDefault(h => h.Id == hotelId) ?? (hotelIds.Count > 0 ? hotels.FirstOrDefault(h => h.Id == hotelIds[0]) : hotels.FirstOrDefault());
        var date = (day ?? today).Date;
        var roomDays = new List<RoomDay>();
        if (shown is not null)
        {
            var dayRows = await (from o in db.Orders.AsNoTracking() join c in db.Users.AsNoTracking() on o.UserId equals c.Id
                where o.HotelId == shown.Id && o.Status != BookingStatus.PaymentReview && o.Status != BookingStatus.Cancelled
                    && o.CheckIn <= date && o.CheckOut >= date
                select new HotelOwnerOrder(o, c.FirstName + " " + c.LastName, c.Email, null)).ToListAsync();
            foreach (var room in await catalog.RoomsAsync(shown.Id, includeInactive: true))
            {
                var night = (await availability.NightsAsync(shown.Id, room.Id, room.Name, date, date.AddDays(1))).Single();
                var stays = dayRows.Where(r => r.Order.RoomType == room.Id || (r.Order.RoomType == null && r.Order.RoomName == room.BookingName))
                    .Select(r => new DayStay(r, r.Order.CheckIn.Date == date ? (r.Order.Status == BookingStatus.Pending ? "wait" : "in")
                        : r.Order.CheckOut.Date == date ? "out" : "stay"))
                    .Where(s => s.Kind != "out" || s.Row.Order.Status is BookingStatus.CheckedIn or BookingStatus.Confirmed or BookingStatus.Completed)
                    .Where(s => s.Kind != "stay" || s.Row.Order.Status == BookingStatus.CheckedIn)
                    .OrderBy(s => s.Kind).ToList();
                roomDays.Add(new RoomDay(room.Id, room.Name, night.Left, night.Capacity, stays));
            }
        }
        return View(new HotelOwnerPage(hotels, rows,
            await entries.OrderByDescending(x => x.Id).Take(200).ToListAsync(),
            await entries.Where(x => x.Kind == "Receipt").SumAsync(x => (decimal?)x.Amount) ?? 0,
            -(await entries.Where(x => x.Kind == "Refund").SumAsync(x => (decimal?)x.Amount) ?? 0),
            pending, status, admin) { Queue = queue, Owed = earned.Sum(x => x.Total) - FinanceRules.CommissionOf(earned), PaidOut = paidOut,
              Summary = summary, Day = date, CalendarHotel = shown, RoomDays = roomDays });
    }
    // Room calendar: rooms for sale per night, rooms taken, and closing dates.
    [HttpGet]
    public async Task<IActionResult> Calendar(string? hotelId, string? month)
    {
        var admin = User.IsInRole(AppRoles.Admin);
        var mine = await db.HotelAssignments.Where(x => x.OwnerUserId == ActorId).Select(x => x.HotelId).Distinct().ToListAsync();
        var hotels = (await catalog.AllAsync()).Where(h => admin || mine.Contains(h.Id)).OrderBy(h => h.Name).ToList();
        var hotel = hotels.FirstOrDefault(h => h.Id == hotelId) ?? hotels.FirstOrDefault();
        var thisMonth = new DateTime(clock.Today.Year, clock.Today.Month, 1);
        var shown = DateTime.TryParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : thisMonth;
        if (shown < thisMonth) shown = thisMonth;
        if (shown > thisMonth.AddMonths(12)) shown = thisMonth.AddMonths(12);
        IReadOnlyList<CalendarRow> rows = hotel is null ? Array.Empty<CalendarRow>() : await availability.MonthAsync(hotel.Id, shown);
        var start = shown > clock.Today ? shown : clock.Today;
        return View(new CalendarPage(hotels, hotel, shown, rows, admin)
        {
            Form = new AllotmentForm { HotelId = hotel?.Id ?? "", RoomType = rows.FirstOrDefault()?.RoomType ?? "", From = start, To = start }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAllotment(AllotmentForm form)
    {
        if (!ModelState.IsValid) TempData["Error"] = "เลือกประเภทห้อง วันเริ่ม และวันสิ้นสุดให้ครบ";
        else
        {
            try
            {
                var nights = await availability.SetAsync(ActorId, form);
                TempData["Success"] = form.Action switch
                {
                    "close" => $"ปิดรับจอง {nights} คืนแล้ว การจองเดิมยังอยู่ครบ",
                    "open" => $"เปิดรับจอง {nights} คืนแล้ว",
                    _ => $"ตั้งจำนวนห้องเป็น {form.Rooms} ห้อง ให้ {nights} คืนแล้ว"
                };
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException e) { TempData["Error"] = e.Message; }
        }
        return RedirectToAction(nameof(Calendar), new { hotelId = form.HotelId, month = form.From.ToString("yyyy-MM") });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id) => await Decide(id, true, null);
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(HotelRejectForm form)
    {
        if (!ModelState.IsValid) return BadRequest("ระบุรายการและเหตุผล 1–120 ตัวอักษร");
        return await Decide(form.Id, false, form.Reason);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id) => await Advance(id, BookingStatus.CheckedIn, "บันทึกเช็กอินแล้ว");
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(int id) => await Advance(id, BookingStatus.Completed, "เช็กเอาต์และปิดรายการแล้ว");
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NoShow(int id) => await Advance(id, BookingStatus.NoShow, "บันทึกว่าลูกค้าไม่มาเข้าพักแล้ว");
    private async Task<IActionResult> Advance(int id, string next, string done)
    {
        try { await hotelDesk.AdvanceAsync(ActorId,id,next); TempData["Success"]=done; }
        catch(UnauthorizedAccessException){return Forbid();}
        catch(ArgumentException ex){TempData["Error"]=ex.Message;}
        // Buttons on the "แขกวันนี้" page send returnTo=Today so the desk stays on that page.
        if (Request.HasFormContentType && Request.Form["returnTo"] == "Today")
            return RedirectToAction(nameof(Today), new { hotelId = Request.Form["hotelId"].ToString() });
        return RedirectToAction(nameof(Index));
    }

    // ---------- Replies to guest reviews ----------
    [HttpGet]
    public async Task<IActionResult> Reviews(string? hotelId, string? filter, [FromServices] ReviewReplyService replies)
    {
        try { return View(await replies.ListAsync(ActorId, User.IsInRole(AppRoles.Admin), hotelId, filter == ReviewReplyPage.Unanswered)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int reviewId, string? body, string? filter, [FromServices] ReviewReplyService replies)
    {
        try { await replies.SaveAsync(ActorId, User.IsInRole(AppRoles.Admin), reviewId, body); TempData["Success"] = "บันทึกคำตอบแล้ว แสดงใต้รีวิวในหน้าที่พัก"; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return await BackToReviews(reviewId, filter);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(int reviewId, string? filter, [FromServices] ReviewReplyService replies)
    {
        try { await replies.DeleteAsync(ActorId, User.IsInRole(AppRoles.Admin), reviewId); TempData["Success"] = "ลบคำตอบแล้ว"; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        return await BackToReviews(reviewId, filter);
    }

    private async Task<IActionResult> BackToReviews(int reviewId, string? filter)
    {
        var hotelId = await db.HotelReviews.Where(x => x.Id == reviewId).Select(x => x.HotelId).SingleOrDefaultAsync();
        return Redirect(Url.Action(nameof(Reviews), new { hotelId, filter = filter == ReviewReplyPage.Unanswered ? filter : ReviewReplyPage.All }) + "#review-" + reviewId);
    }

    // Front desk for today: check guests in, check them out, see who is staying.
    [HttpGet]
    public async Task<IActionResult> Today(string? hotelId)
    {
        var admin = User.IsInRole(AppRoles.Admin);
        var mine = await db.HotelAssignments.Where(x => x.OwnerUserId == ActorId).Select(x => x.HotelId).Distinct().ToListAsync();
        var hotels = (await catalog.AllAsync(includeInactive: true)).Where(h => admin || mine.Contains(h.Id)).OrderBy(h => h.Name).ToList();
        if (!string.IsNullOrEmpty(hotelId) && hotels.All(h => h.Id != hotelId)) return Forbid();
        var hotel = hotels.FirstOrDefault(h => h.Id == hotelId) ?? hotels.FirstOrDefault();
        var board = new GuestBoard([], [], []);
        if (hotel is not null)
        {
            var today = clock.Today;
            var rows = await (from o in db.Orders.AsNoTracking()
                              join u in db.Users.AsNoTracking() on o.UserId equals u.Id
                              where o.HotelId == hotel.Id
                                 && ((o.Status == BookingStatus.Confirmed && o.CheckIn <= today) || o.Status == BookingStatus.CheckedIn)
                              select new GuestRow(o, u.FirstName + " " + u.LastName, u.Email)).ToListAsync();
            board = GuestBoard.Split(rows, today);
        }
        return View(new TodayGuestsPage(hotels, hotel, board));
    }
    private async Task<IActionResult> Decide(int id, bool confirm, string? reason)
    {
        try {
            await hotelDesk.DecideAsync(ActorId, id, confirm, reason);
            TempData["Success"] = confirm ? "ยืนยันการจองแล้ว ลูกค้าเห็นสถานะใหม่ทันที" : "ยกเลิกการจอง คืนเครดิตและคืนสิทธิ์โค้ดให้ลูกค้าแล้ว";
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }

    // ---------- Ask for a hotel, with proof of working there (Admin or Manager decides) ----------
    [HttpGet]
    public async Task<IActionResult> Access([FromServices] HotelAccessService access)
    {
        var mine = await db.HotelAssignments.AsNoTracking().Where(x => x.OwnerUserId == ActorId).Select(x => x.HotelId).ToListAsync();
        var hotels = (await catalog.AllAsync()).Where(h => !mine.Contains(h.Id)).OrderBy(h => h.Name).ToList();
        return View(new HotelAccessPage(hotels, await access.MineAsync(ActorId)));
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(UploadedFiles.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Access(string? hotelId, string? message, IFormFile? proof,
        [FromServices] HotelAccessService access, [FromServices] IWebHostEnvironment env)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hotelId)) throw new ArgumentException("เลือกโรงแรมที่ต้องการดูแล");
            if (proof is null) throw new ArgumentException("แนบหลักฐานว่าเป็นพนักงานของโรงแรมนี้ (" + UploadedFiles.Rule + ")");
            if (await db.HotelAccessRequests.AnyAsync(x => x.UserId == ActorId && x.HotelId == hotelId && x.Status == AccessRequestStatus.Pending))
                throw new ArgumentException("มีคำขอของโรงแรมนี้รอพิจารณาอยู่แล้ว");
            var (name, error) = await UploadedFiles.SaveAsync(proof, env, "HotelProofs");
            if (error is not null) throw new ArgumentException("หลักฐาน: " + error);
            try { await access.SubmitAsync(ActorId, hotelId, message, name!); }
            catch
            {
                // The request was not saved, so neither is its file.
                if (UploadedFiles.PathOf(env, "HotelProofs", name) is string path) System.IO.File.Delete(path);
                throw;
            }
            TempData["Success"] = "ส่งคำขอแล้ว Admin หรือ Manager จะตรวจหลักฐานและแจ้งผลทางการแจ้งเตือน";
        }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Access));
    }
}
