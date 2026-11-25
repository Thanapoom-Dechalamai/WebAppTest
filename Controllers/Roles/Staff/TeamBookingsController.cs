using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Policy = RolePolicies.Staff)]
[AutoValidateAntiforgeryToken]
[Route("TeamBookings")]
public sealed class TeamBookingsController(ApplicationDbContext db, CatalogService catalog, CheckoutService checkout,
    CancellationService cancellation, TeamBookingEdits edits, SupportQuoteService quotes, IThaiClock clock) : Controller
{
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("Case/{id:int}")]
    public async Task<IActionResult> Case(int id)
    {
        var page = await PageAsync(id);
        return page is null ? NotFound() : View(page);
    }

    [HttpGet("Rooms")]
    public async Task<IActionResult> Rooms(string hotelId) =>
        Json((await catalog.RoomsAsync(hotelId)).Select(room => new { room.Id, room.Name }));

    // Check price and availability before sending (nothing is saved).
    [HttpPost("Quote/{id:int}")]
    public async Task<IActionResult> Quote(int id, [Bind(Prefix = "Booking")] BookingForm form)
    {
        var page = await PageAsync(id, form);
        if (page is null) return NotFound();
        form.ExpectedTotal = null;
        ModelState.Remove("Booking.ExpectedTotal");
        if (!page.CanChange || !ModelState.IsValid) return View("Case", page);
        try
        {
            var subtotal = await checkout.QuoteAsync(form);
            form.ExpectedTotal = (await checkout.OfferAsync(page.Case.CustomerId, form, subtotal)).Total;
        }
        catch (ArgumentException error) { ModelState.AddModelError("", error.Message); }
        return View("Case", page);
    }

    // Send the checked stay to the customer as a quote (B1). The customer accepts and pays in the ticket; nothing is charged here.
    [HttpPost("SendQuote/{id:int}")]
    public async Task<IActionResult> SendQuote(int id, [Bind(Prefix = "Booking")] BookingForm form)
    {
        var page = await PageAsync(id, form);
        if (page is null) return NotFound();
        if (!page.CanChange || !ModelState.IsValid) return View("Case", page);
        try
        {
            var quote = await quotes.SendAsync(ActorId, id, form);
            TempData["Success"] = $"ส่งใบเสนอราคา {QuoteRules.Number(quote.Id)} ยอด ฿{quote.Total:N2} ให้ลูกค้าแล้ว รอลูกค้ายืนยันและชำระ";
            return LocalRedirect(SupportTicketService.StaffUrl(id));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException error)
        {
            ModelState.AddModelError("", error.Message);
            form.ExpectedTotal = null;
            ModelState.Remove("Booking.ExpectedTotal");
            return View("Case", page);
        }
    }

    [HttpPost("Cancel/{id:int}")]
    public async Task<IActionResult> Cancel(int id, int orderId, string? reason)
    {
        var page = await PageAsync(id);
        if (page is null || page.Order?.Id != orderId) return NotFound();
        if (!page.CanChange) return BadRequest("เรื่องนี้ปิดแล้ว");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 120)
            return BadRequest("ระบุเหตุผล 1–120 ตัวอักษร");
        try
        {
            await cancellation.CancelForCaseAsync(ActorId, id, orderId, page.Case.CustomerId, "เจ้าหน้าที่ยกเลิกตามคำขอ: " + reason.Trim());
            TempData["Success"] = "ยกเลิกแล้ว ระบบคืนเครดิตสำหรับยอดที่รับชำระแล้ว";
        }
        catch (ArgumentException error) { TempData["Error"] = error.Message; }
        return RedirectToAction(nameof(Case), new { id });
    }

    [HttpPost("Edit/{id:int}"), RequestSizeLimit(UploadedFiles.MaxBytes + 256 * 1024)]
    public async Task<IActionResult> Edit(int id, TeamBookingEdit form)
    {
        var page = await PageAsync(id);
        if (page is null || page.Order?.Id != form.OrderId) return NotFound();
        if (!page.CanChange || !ModelState.IsValid) return BadRequest("ข้อมูลที่ขอแก้ไขไม่ถูกต้อง");
        try
        {
            await edits.SaveAsync(ActorId, id, page.Case.CustomerId, form);
            TempData["Success"] = "บันทึกข้อมูลการจองแล้ว";
        }
        catch (ArgumentException error) { TempData["Error"] = error.Message; }
        return RedirectToAction(nameof(Case), new { id });
    }

    private async Task<TeamBookingPage?> PageAsync(int id, BookingForm? form = null)
    {
        var item = await db.SupportCases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (item is null) return null;
        var customer = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.CustomerId);
        if (customer is null) return null;
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.OrderId && x.UserId == item.CustomerId);
        var hotels = await catalog.AllAsync();
        form ??= new BookingForm
        {
            HotelId = item.HotelId ?? order?.HotelId ?? hotels.FirstOrDefault()?.Id ?? "",
            RoomType = item.RoomType ?? order?.RoomType ?? "deluxe",
            CheckIn = item.CheckIn ?? clock.Today.AddDays(1),
            CheckOut = item.CheckOut ?? clock.Today.AddDays(2),
            Rooms = item.Rooms ?? order?.Rooms ?? 1, Guests = item.Guests ?? order?.Guests ?? 2,
            PromotionCode = item.PromotionCode, SpecialRequest = item.SpecialRequest
        };
        var canChange = item.Status != SupportStatus.Closed && customer.Role == AppRoles.Customer && !customer.IsSuspended;
        return new(item, customer.FirstName + " " + customer.LastName, order, form, hotels,
            await catalog.RoomsAsync(form.HotelId), canChange, order is not null && TeamBookingEdits.EvidenceCanChange(order))
        {
            Quotes = await db.SupportQuotes.AsNoTracking().Where(x => x.CaseId == id).OrderByDescending(x => x.Id).ToListAsync()
        };
    }
}
