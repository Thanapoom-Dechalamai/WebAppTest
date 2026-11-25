using System.Globalization;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Support;

// The price support has checked for one stay, as the quote stores it.
public sealed record QuotePrice(string HotelName, string RoomName, decimal Subtotal, decimal Discount, decimal Total, string? Code);

// How the customer pays when accepting a quote (Credit, or Transfer with a reference and/or a saved slip).
public sealed record QuotePayment(string Method, string? PaymentRef, string? SlipFile);

// Pure quote rules (owner's choices 2026-11-25): valid 24 hours, or until the check-in day starts if that is sooner;
// nothing is held while it waits; a newer quote replaces the older open one. Unit tested in QuoteRulesTests.
public static class QuoteRules
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    public const int MaxDeclineReason = 300;
    private static readonly TimeSpan ThaiOffset = TimeSpan.FromHours(7);

    public static string Number(int id) => "QT" + id.ToString("D6", CultureInfo.InvariantCulture);

    // The earlier of "24 hours after sending" and "midnight Thai time at the start of the check-in day".
    public static DateTime ExpiresAt(DateTime createdUtc, DateTime checkIn)
    {
        var checkInStartsUtc = DateTime.SpecifyKind(checkIn.Date - ThaiOffset, DateTimeKind.Utc);
        var dayLimit = DateTime.SpecifyKind(createdUtc, DateTimeKind.Utc) + Lifetime;
        return dayLimit < checkInStartsUtc ? dayLimit : checkInStartsUtc;
    }

    // Expired is worked out from the time, never stored.
    public static string StateOf(SupportQuote quote, DateTime nowUtc) =>
        quote.Status == QuoteStatus.Open && nowUtc >= quote.ExpiresAtUtc ? QuoteStatus.Expired : quote.Status;

    public static bool IsOpen(SupportQuote quote, DateTime nowUtc) => StateOf(quote, nowUtc) == QuoteStatus.Open;

    // Null when the customer may accept or decline it now; otherwise the Thai reason.
    public static string? DecisionError(SupportQuote quote, DateTime nowUtc) => StateOf(quote, nowUtc) switch
    {
        QuoteStatus.Open => null,
        QuoteStatus.Expired => "ใบเสนอราคานี้หมดอายุแล้ว ขอใบใหม่ในแชตได้เลย",
        QuoteStatus.Accepted => "ใบเสนอราคานี้ยืนยันไปแล้ว",
        QuoteStatus.Declined => "คุณไม่รับใบเสนอราคานี้แล้ว ขอใบใหม่ในแชตได้",
        _ => "มีใบเสนอราคาใหม่แทนใบนี้แล้ว ดูใบล่าสุดในแชต"
    };

    public static SupportQuote Create(SupportCase ticket, int actorId, BookingForm form, QuotePrice price, DateTime nowUtc) => new()
    {
        CaseId = ticket.Id, CustomerId = ticket.CustomerId, CreatedByUserId = actorId, RequestId = Guid.NewGuid().ToString(),
        HotelId = form.HotelId, HotelName = price.HotelName, RoomType = form.RoomType, RoomName = price.RoomName,
        CheckIn = form.CheckIn.Date, CheckOut = form.CheckOut.Date, Rooms = form.Rooms, Guests = form.Guests,
        PromotionCode = price.Code, SpecialRequest = BookingRules.CleanRequest(form.SpecialRequest),
        Subtotal = price.Subtotal, Discount = price.Discount, Total = price.Total,
        Status = QuoteStatus.Open, CreatedAtUtc = nowUtc, ExpiresAtUtc = ExpiresAt(nowUtc, form.CheckIn)
    };

    // The booking the customer makes by accepting: exactly the quoted stay, at the quoted total (a changed price is refused).
    public static BookingForm ToBookingForm(SupportQuote quote, QuotePayment payment) => new()
    {
        HotelId = quote.HotelId, RoomType = quote.RoomType, CheckIn = quote.CheckIn, CheckOut = quote.CheckOut,
        Rooms = quote.Rooms, Guests = quote.Guests, PromotionCode = quote.PromotionCode, SpecialRequest = quote.SpecialRequest,
        RequestId = quote.RequestId, ExpectedTotal = quote.Total, PaymentMethod = PaymentRules.MethodOf(payment.Method),
        PaymentRef = payment.PaymentRef, SlipFile = payment.SlipFile
    };

    public static string? CleanReason(string? reason)
    {
        var text = string.Join(' ', (reason ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length == 0 ? null : text.Length <= MaxDeclineReason ? text : text[..MaxDeclineReason];
    }

    // Chat lines that record each step in the ticket.
    public static string SentMessage(SupportQuote quote) =>
        $"ส่งใบเสนอราคา {Number(quote.Id)} · {quote.HotelName} · {quote.RoomName} · {Stay(quote)} · ยอดชำระ ฿{quote.Total:N2} " +
        $"กดยืนยันและเลือกวิธีชำระได้ในแชตนี้ ภายใน {Thai(quote.ExpiresAtUtc)}";

    public static string AcceptedMessage(SupportQuote quote, int orderId, string paymentMethod) =>
        $"ยืนยันใบเสนอราคา {Number(quote.Id)} ชำระด้วย{(PaymentRules.IsTransfer(paymentMethod) ? "การโอนเงิน" : "เครดิต")} · การจอง {BookingText.Number(orderId)}";

    public static string DeclinedMessage(SupportQuote quote, string? reason) =>
        $"ไม่รับใบเสนอราคา {Number(quote.Id)}" + (CleanReason(reason) is string why ? $" · เหตุผล: {why}" : "");

    private static string Stay(SupportQuote quote) =>
        $"{ThaiDates.Short(quote.CheckIn)} – {ThaiDates.Short(quote.CheckOut)} · {quote.Rooms} ห้อง {quote.Guests} คน";

    private static string Thai(DateTime utc) { var local = utc.AddHours(7); return $"{ThaiDates.Short(local)} {local:HH:mm} น."; }
}
