using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

// Customer side of light support: "my cases", open a case, read the conversation, reply, close.
[Authorize(Policy = RolePolicies.Customer)]
public class SupportController(SupportService support, CatalogService catalog, HotelRoomCatalog rooms, SupportQuoteService quotes,
    IWebHostEnvironment environment) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The site's default action is Login, so /Support needs its own route.
    [HttpGet("/Support"), HttpGet("/Support/Index")]
    public async Task<IActionResult> Index() => View(await support.ForCustomerAsync(UserId));

    [HttpGet]
    public async Task<IActionResult> New([FromQuery] NewCaseForm form)
    {
        var page = await CaseFormAsync(form);
        if (form.OrderId is not null && page.Order is null) return NotFound();
        if (page.RoomName is not null && string.IsNullOrWhiteSpace(form.Subject)) form.Subject = "ขอจอง " + page.RoomName;
        ModelState.Clear();
        return View(page);
    }

    [HttpPost, ActionName(nameof(New)), ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadedFiles.MaxBytes + 256 * 1024)]
    public async Task<IActionResult> Open(NewCaseForm form)
    {
        var page = await CaseFormAsync(form);
        if (form.HotelId is not null && page.RoomName is null)
            ModelState.AddModelError(nameof(form.HotelId), "ไม่พบที่พักหรือประเภทห้องที่เลือก");
        if (ModelState.IsValid)
        {
            try { var id = await support.OpenAsync(UserId, form); TempData["Success"] = "ส่งเรื่องถึง Lodgo support แล้ว เมื่อมีคำตอบคุณจะได้รับแจ้งเตือน"; return RedirectToAction(nameof(Case), new { id }); }
            catch (ArgumentException e) { ModelState.AddModelError("", e.Message); }
        }
        return View(page);
    }

    private async Task<NewCasePage> CaseFormAsync(NewCaseForm form)
    {
        var linked = await support.OrderInfoAsync(form.OrderId, UserId);
        var hotel = form.HotelId is null ? null : await catalog.FindAsync(form.HotelId);
        var room = hotel is not null && form.RoomType is not null ? await rooms.FindAsync(hotel.Id, form.RoomType) : null;
        return new(form, linked, hotel?.Name, room is { IsActive: true } ? room.Name : null);
    }

    [HttpGet]
    public async Task<IActionResult> Case(int id)
    {
        var view = await support.GetAsync(id, UserId);
        return view is null ? NotFound() : View(view with { Inbox = await support.ForCustomerAsync(UserId) });
    }

    [HttpGet]
    public async Task<IActionResult> Messages(int id)
    {
        var view = await support.GetAsync(id, UserId);
        return view is null ? NotFound() : PartialView("_Messages", view);
    }

    [HttpGet]
    public async Task<IActionResult> Attachment(int id)
    {
        var (onRecord, path) = await support.AttachmentAsync(id, UserId);
        if (path is null) return onRecord ? this.FileMissing() : NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, UploadedFiles.ContentType(path), "lodgo-attachment" + Path.GetExtension(path));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadedFiles.MaxBytes + 256 * 1024)]
    public async Task<IActionResult> Reply(int id, string? body, IFormFile? attachment)
    {
        try { await support.ReplyAsync(UserId, id, body, fromStaff: false, attachment); TempData["Success"] = "ส่งข้อความแล้ว"; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Case), new { id });
    }

    // Accept a quote from support: the booking is made now, with the payment method chosen here (B1, 2026-11-25).
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadedFiles.MaxBytes + 256 * 1024)]
    public async Task<IActionResult> AcceptQuote(QuoteAcceptForm form)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "ข้อมูลการชำระไม่ถูกต้อง"; return RedirectToAction(nameof(Case), new { id = form.CaseId }); }
        string? slip = null;
        try
        {
            if (PaymentRules.IsTransfer(form.PaymentMethod) && form.Slip is not null)
                slip = await TeamBookingEdits.SaveSlipAsync(form.Slip, environment);
            var (orderId, _) = await quotes.AcceptAsync(UserId, form.QuoteId,
                new QuotePayment(form.PaymentMethod, PaymentRules.CleanPaymentRef(form.PaymentRef), slip));
            if (slip is not null && !await support.OrderHasSlipAsync(orderId, slip)) TeamBookingEdits.RemoveSlip(environment, slip);
            TempData["Success"] = "ยืนยันแล้ว ฝ่ายบัญชีจะตรวจการชำระเงินก่อนส่งถึงโรงแรม";
            return RedirectToAction("Order", "Stays", new { id = orderId.ToString() });
        }
        catch (KeyNotFoundException) { TeamBookingEdits.RemoveSlip(environment, slip); return NotFound(); }
        catch (ArgumentException e)
        {
            TeamBookingEdits.RemoveSlip(environment, slip);
            TempData["Error"] = e.Message;
            return RedirectToAction(nameof(Case), new { id = form.CaseId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineQuote(int quoteId, int caseId, string? reason)
    {
        try { caseId = await quotes.DeclineAsync(UserId, quoteId, reason); TempData["Success"] = "แจ้งเจ้าหน้าที่แล้วว่าคุณไม่รับใบเสนอราคานี้"; }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Case), new { id = caseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        try { await support.CloseAsync(UserId, id, asStaff: false); TempData["Success"] = "ปิดเรื่องแล้ว ขอบคุณที่แจ้งให้เราทราบ"; }
        catch (KeyNotFoundException) { return NotFound(); }
        return RedirectToAction(nameof(Case), new { id });
    }
}
