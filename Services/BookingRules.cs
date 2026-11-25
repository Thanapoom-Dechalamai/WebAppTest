using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services;

/// <summary>
/// Booking rules that need nothing but their inputs: no database, no clock, no I/O.
/// "Today" is always passed in (from <see cref="IThaiClock"/>), so every rule gives the same answer for the same input
/// and can be unit-tested for any date. The booking workflows apply these rules inside their transactions.
/// </summary>
public static class BookingRules
{
    public const int MaxNights = 30;
    public const int MaxRooms = 5;
    public const int GuestsPerRoom = 2;
    public const int MaxSpecialRequestLength = 300;

    public const string StayDatesRule = "เลือกวันเข้าพักตั้งแต่พรุ่งนี้ ภายใน 1 ปี และพักได้ 1–30 คืน";
    public const string PartyRule = "เลือก 1–5 ห้อง และผู้เข้าพักไม่เกิน 2 คนต่อห้อง";
    public const string CancelRuleMessage = "ยกเลิกได้เฉพาะรายการรอยืนยัน หรือรายการยืนยันแล้วก่อนวันเช็กอิน";

    /// <summary>Check-in from tomorrow up to one year ahead; 1 to <see cref="MaxNights"/> nights. Times of day are ignored.
    /// Not today (owner, 2026-11-25): an unconfirmed booking is cancelled when its check-in day arrives (BookingLifecycle),
    /// so a same-day booking could never be confirmed in time.</summary>
    public static bool StayDatesValid(DateTime checkIn, DateTime checkOut, DateTime today)
    {
        var inDay = checkIn.Date; var outDay = checkOut.Date; var nights = (outDay - inDay).Days;
        return inDay > today.Date && inDay <= today.Date.AddYears(1) && nights is >= 1 and <= MaxNights;
    }

    /// <summary>1 to <see cref="MaxRooms"/> rooms, at least one guest, at most <see cref="GuestsPerRoom"/> guests per room.</summary>
    public static bool PartyValid(int rooms, int guests) =>
        rooms is >= 1 and <= MaxRooms && guests >= 1 && guests <= rooms * GuestsPerRoom;

    /// <summary>A promotion code can be used today: switched on, inside its dates (both days included) and worth something.</summary>
    public static bool PromotionOpen(Promotion promotion, DateTime today) =>
        promotion.IsActive && promotion.StartsOn.Date <= today.Date && promotion.EndsOn.Date >= today.Date && promotion.DiscountValue > 0;

    /// <summary>
    /// One cancellation rule for everyone (customer, hotel staff, Lodgo support, Admin): a request can be cancelled while it
    /// waits (for the accounts check or the hotel), a confirmed stay only before its check-in day.
    /// </summary>
    public static bool CanCancel(StayOrder order, DateTime today) =>
        order.Status is BookingStatus.Pending or BookingStatus.PaymentReview
        || (order.Status == BookingStatus.Confirmed && order.CheckIn.Date > today.Date);

    /// <summary>Special request: blank becomes null; longer text is cut to the column limit even if form validation was skipped.</summary>
    public static string? CleanRequest(string? text)
    {
        text = text?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length > MaxSpecialRequestLength ? text[..MaxSpecialRequestLength] : text;
    }

    /// <summary>Demo mode: the staff step a customer may do next on their own booking, or null when none is left.</summary>
    public static string? DemoNextStep(StayOrder order) => order.Status switch
    {
        BookingStatus.PaymentReview => "ฝ่ายบัญชีตรวจเงิน",
        BookingStatus.Pending => "โรงแรมยืนยันห้อง",
        BookingStatus.Confirmed => "เช็กอิน",
        BookingStatus.CheckedIn => "เช็กเอาต์",
        _ => null
    };
}
