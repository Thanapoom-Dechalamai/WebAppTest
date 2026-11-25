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

namespace Nontiwat_7362_L002.Controllers;

// Booking is a customer action. Staff accounts use their own workspace, so they cannot
// book a hotel and then confirm that same booking themselves.
public class StaysController(ApplicationDbContext db, CancellationService cancellation, CatalogService catalog, ReviewService reviews,
    HotelPhotoService photos, IConfiguration config) : Controller
{

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The "ที่พัก" page: all hotels as a list. Anyone may browse; filters and sort come from the query string.
    [HttpGet("/Stays"), AllowAnonymous]
    public async Task<IActionResult> Index(string? destination, string? q, string? category, int? minPrice, int? maxPrice, double? rating,
        string? sort, int page = 1, string? checkin = null, string? checkout = null, int? guests = null, int? rooms = null, string? promo = null)
    {
        var all = await catalog.AllAsync();
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];
        sort = sort is not null && StayListQuery.Sorts.ContainsKey(sort) ? sort : "recommended";
        if (minPrice is < 0) minPrice = null;
        if (maxPrice is < 0) maxPrice = null;
        if (minPrice is not null && maxPrice is not null && minPrice > maxPrice) (minPrice, maxPrice) = (maxPrice, minPrice);
        if (rating is not null && !StayListQuery.Ratings.Contains(rating.Value)) rating = null;
        if (category is not null && !StayEnvironmentOptions.DiscoveryCategories.ContainsKey(category)) category = null;
        IEnumerable<HotelCardViewModel> list = all.Where(h =>
            (string.IsNullOrEmpty(destination) || h.City == destination) &&
            (q is null || h.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || h.City.Contains(q, StringComparison.OrdinalIgnoreCase)) &&
            HotelMatching.InCategory(h, category) &&
            (minPrice is null || h.StartingRate >= minPrice) && (maxPrice is null || h.StartingRate <= maxPrice) &&
            (rating is null || (h.ReviewScore ?? 0) >= rating));
        list = sort switch
        {
            "price-asc" => list.OrderBy(h => h.StartingRate is null).ThenBy(h => h.StartingRate),
            "price-desc" => list.OrderByDescending(h => h.StartingRate),
            "rating" => list.OrderByDescending(h => h.ReviewScore ?? 0).ThenByDescending(h => h.ReviewCount),
            _ => list
        };
        var found = list.ToList();
        var pages = Math.Max(1, (int)Math.Ceiling(found.Count / (double)StayListPage.PageSize));
        page = Math.Clamp(page, 1, pages);
        var cities = all.Select(h => h.City).Distinct().OrderBy(c => c, StringComparer.Create(new System.Globalization.CultureInfo("th-TH"), false)).ToList();
        promo = string.IsNullOrWhiteSpace(promo) ? null : promo.Trim().ToUpperInvariant()[..Math.Min(promo.Trim().Length, 40)];
        var query = new StayListQuery(destination, q, category, minPrice, maxPrice, rating, sort, page, checkin, checkout, guests, rooms, promo);
        return View(new StayListPage(query, found.Skip((page - 1) * StayListPage.PageSize).Take(StayListPage.PageSize).ToList(),
            found.Count, pages, cities, (int)Math.Ceiling(all.Max(h => h.StartingRate) ?? 0), all));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, int? rstars, string? rtype, string? rsort, int rpage = 1)
    {
        var hotel = await catalog.FindAsync(id);
        if (hotel is null) return NotFound();
        var environment = await db.StayEnvironments.AsNoTracking().SingleOrDefaultAsync(x => x.HotelId == id)
            ?? new Nontiwat_7362_L002.Models.StayEnvironment { HotelId = id };
        var section = await reviews.SectionAsync(id, new ReviewQuery(rstars, rtype, rsort is "high" or "low" ? rsort : "recent", rpage));
        var rooms = (await catalog.RoomsAsync(hotel.Id)).OrderBy(room => room.NightlyRate(hotel.Price)).ToList();
        return View(new HotelPage(hotel with { Environment = environment }, rooms)
        {
            Photos = await photos.ListAsync(hotel.Id), Reviews = section, Invite = await ReviewInviteAsync(id)
        });
    }

    // Reviews come only from real stays: a finished booking at this hotel that has no review yet.
    private async Task<ReviewInvite?> ReviewInviteAsync(string hotelId)
    {
        if (!User.IsInRole(AppRoles.Customer) || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var me)) return null;
        var mine = await db.Orders.AsNoTracking().Where(x => x.UserId == me && x.HotelId == hotelId)
            .Select(x => new { x.Id, x.Status, x.CheckOut }).ToListAsync();
        var ids = mine.Select(x => x.Id).ToList();
        var reviewed = (await db.HotelReviews.AsNoTracking().Where(x => x.OrderId != null && ids.Contains(x.OrderId.Value)).Select(x => x.OrderId!.Value).ToListAsync()).ToHashSet();
        var ready = mine.Where(x => x.Status == BookingStatus.Completed && !reviewed.Contains(x.Id)).OrderByDescending(x => x.CheckOut).FirstOrDefault();
        if (ready is not null) return new ReviewInvite(ReviewInvite.Ready, ready.Id);
        if (mine.Any(x => x.Status is BookingStatus.PaymentReview or BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.CheckedIn)) return new ReviewInvite(ReviewInvite.Upcoming);
        return new ReviewInvite(mine.Any(x => reviewed.Contains(x.Id)) ? ReviewInvite.Reviewed : ReviewInvite.None);
    }

    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public IActionResult Checkout(BookingForm form) => RedirectToAction("New", "Support", new
    {
        form.HotelId, form.RoomType, CheckIn = form.CheckIn.ToString("yyyy-MM-dd"), CheckOut = form.CheckOut.ToString("yyyy-MM-dd"),
        form.Rooms, form.Guests, form.PromotionCode, form.SpecialRequest
    });

    // A transfer slip: the customer who booked, Lodgo staff, AC and Admin may see it. Hotels never do.
    [Authorize, HttpGet]
    public async Task<IActionResult> Slip(int id, [FromServices] IWebHostEnvironment env)
    {
        var order = await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.UserId, x.SlipFile }).SingleOrDefaultAsync();
        if (order is null) return NotFound();
        var allowed = order.UserId == UserId || User.IsInRole(AppRoles.Staff) || User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.AC);
        if (!allowed) return Forbid();
        var path = UploadedFiles.PathOf(env, "Slips", order.SlipFile);
        if (path is null) return order.SlipFile is null ? NotFound() : this.FileMissing();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, UploadedFiles.ContentType(path));
    }

    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> Bookings(string? tab, string? q, string? sort)
    {
        await cancellation.ExpireStalePendingAsync();
        tab = BookingTabs.Normalize(tab);
        sort = sort == "booked" ? "booked" : "checkin";
        var mine = db.Orders.AsNoTracking().Where(x => x.UserId == UserId);
        var counts = await mine.GroupBy(x => x.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        int CountOf(string t) => counts.Where(x => BookingTabs.Statuses(t).Contains(x.Status)).Sum(x => x.Count);
        var statuses = BookingTabs.Statuses(tab);
        var query = mine.Where(x => statuses.Contains(x.Status));
        q = q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            if (q.Length > 100) q = q[..100];
            var digits = q.StartsWith("LG", StringComparison.OrdinalIgnoreCase) ? q[2..] : q;
            query = int.TryParse(digits, out var number)
                ? query.Where(x => x.Id == number)
                : query.Where(x => x.HotelName.Contains(q) || x.City.Contains(q));
        }
        query = sort == "booked" ? query.OrderByDescending(x => x.Id)
            : tab == BookingTabs.Upcoming ? query.OrderBy(x => x.CheckIn).ThenBy(x => x.Id)
            : query.OrderByDescending(x => x.CheckIn).ThenByDescending(x => x.Id);
        var orders = await query.Take(200).ToListAsync();
        var hotels = (await catalog.AllAsync()).ToDictionary(x => x.Id);
        var ids = orders.Select(x => x.Id).ToList();
        var reviewed = (await db.HotelReviews.AsNoTracking().Where(x => x.OrderId != null && ids.Contains(x.OrderId.Value))
            .Select(x => x.OrderId!.Value).ToListAsync()).ToHashSet();
        return View(new BookingListPage(orders, hotels, tab, q, sort,
            CountOf(BookingTabs.Upcoming), CountOf(BookingTabs.Finished), CountOf(BookingTabs.Cancelled)) { ReviewedOrderIds = reviewed });
    }

    // Printable booking confirmation. Only the guest who booked (or Admin); anyone else gets 404
    // so a guessed number does not reveal that the booking exists.
    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        var admin = User.IsInRole(AppRoles.Admin);
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (admin || x.UserId == UserId));
        if (order is null) return NotFound();
        var guest = await db.Users.AsNoTracking().SingleAsync(x => x.Id == order.UserId);
        var room = (await catalog.RoomsAsync()).FirstOrDefault(x => order.RoomType is null ? x.Name == order.RoomName : x.Id == order.RoomType && x.Name == order.RoomName);
        return View(new BookingDetailPage(order, await catalog.FindAsync(order.HotelId), room,
            $"{guest.FirstName} {guest.LastName}", guest.Email, null, 0));
    }

    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> Order(int id)
    {
        await cancellation.ExpireStalePendingAsync();
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == UserId && x.Id == id);
        if (order is null) return NotFound();
        var guest = await db.Users.AsNoTracking().SingleAsync(x => x.Id == UserId);
        // Tell the customer who is handling the request right now.
        var teamSize = await (from a in db.HotelAssignments.AsNoTracking() join u in db.Users.AsNoTracking() on a.OwnerUserId equals u.Id
                              where a.HotelId == order.HotelId && u.Role == AppRoles.HotelOwner select a).CountAsync();
        var note = await db.HotelDecisions.AsNoTracking().Where(x => x.OrderId == id).Select(x => x.Note).SingleOrDefaultAsync();
        var room = (await catalog.RoomsAsync()).FirstOrDefault(x => order.RoomType is null ? x.Name == order.RoomName : x.Id == order.RoomType && x.Name == order.RoomName);
        var timeline = (await db.BookingEvents.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Id).ToListAsync())
            .Select(x => new TimelineItem(x.CreatedAtUtc, x.Status, x.Note, x.ActorUserId == UserId ? "คุณ" : x.ActorLabel)).ToList();
        if (timeline.Count == 0)
        {
            // Bookings made before the history existed: show what is known.
            timeline.Add(new TimelineItem(order.CreatedAtUtc, BookingStatus.Pending, "ส่งคำขอจองและชำระด้วยเครดิต", "คุณ"));
            var last = await db.HotelDecisions.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id);
            if (last is not null) timeline.Add(new TimelineItem(last.UpdatedAtUtc, last.Action, last.Note, "โรงแรม / ระบบ"));
        }
        // Reading the booking counts as reading its notifications.
        await db.Notifications.Where(x => x.UserId == UserId && x.OrderId == id && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
        return View(new BookingDetailPage(order, await catalog.FindAsync(order.HotelId), room,
            $"{guest.FirstName} {guest.LastName}", guest.Email, note, teamSize)
            {
                Reviewed = await db.HotelReviews.AnyAsync(x => x.OrderId == id),
                Timeline = timeline,
                DemoStep = config.GetValue<bool>("Demo:StaySimulation") ? StayDemoService.NextStepLabel(order.Status) : null
            });
    }

    [Authorize(Policy = RolePolicies.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateNext(int id, [FromServices] StayDemoService demo)
    {
        if (!config.GetValue<bool>("Demo:StaySimulation") ||
            !await db.Orders.AnyAsync(x => x.Id == id && x.UserId == UserId))
            return NotFound();

        try { TempData["Success"] = await demo.SimulateNextStepAsync(UserId, id); }
        catch (ArgumentException error) { TempData["Error"] = error.Message; }
        return RedirectToAction(nameof(Order), new { id });
    }

    // Write a review for a completed stay (one per booking).
    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var order = await reviews.ReviewableOrderAsync(UserId, id);
        if (order is null) { TempData["Error"] = "รีวิวได้เฉพาะการเข้าพักที่เสร็จสิ้นแล้ว และรีวิวได้ครั้งเดียวต่อการจอง"; return RedirectToAction(nameof(Order), new { id }); }
        return View(new ReviewFormPage(order, await catalog.FindAsync(order.HotelId), new ReviewForm { OrderId = id }));
    }

    [Authorize(Policy = RolePolicies.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewForm form)
    {
        var order = await reviews.ReviewableOrderAsync(UserId, form.OrderId);
        if (order is null) { TempData["Error"] = "การจองนี้รีวิวไม่ได้ หรือรีวิวไปแล้ว"; return RedirectToAction(nameof(Bookings), new { tab = BookingTabs.Finished }); }
        if (!ModelState.IsValid) return View(new ReviewFormPage(order, await catalog.FindAsync(order.HotelId), form));
        try { await reviews.CreateAsync(UserId, form); }
        catch (ArgumentException e) { ModelState.AddModelError("", e.Message); return View(new ReviewFormPage(order, await catalog.FindAsync(order.HotelId), form)); }
        TempData["Success"] = "ขอบคุณสำหรับรีวิว รีวิวของคุณแสดงในหน้าที่พักแล้ว";
        return RedirectToAction(nameof(Details), null, new { id = order.HotelId, rsort = "recent" }, "reviews");
    }

}
