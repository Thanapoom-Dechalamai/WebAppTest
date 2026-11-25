using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// What a stay costs: nights, subtotal and the discount a promotion code gives.
/// Pure: no database, no clock, no I/O — "today" is always passed in, so the same input always gives the same answer
/// and every case can be unit-tested. The database work (reading the hotel, the room and the promotion, and checking
/// whether this customer already used the code) stays in the service that calls these methods.
/// </summary>
public static class BookingPricing
{
    /// <summary>Price problem found while quoting, where the customer can still be told to contact support.</summary>
    public const string QuoteRateRule = "ราคาที่พักไม่ถูกต้อง กรุณาติดต่อผู้ดูแล";
    /// <summary>Price problem found while booking, under the row locks.</summary>
    public const string BookingRateRule = "ราคาที่พักไม่ถูกต้อง";
    public const string PromotionClosedRule = "โค้ดไม่ถูกต้อง ยังไม่เริ่มใช้ หรือหมดอายุแล้ว";
    public const string PromotionKindRule = "รูปแบบส่วนลดไม่ถูกต้อง";
    public const string PromotionNoDiscountRule = "โค้ดนี้ยังไม่มีส่วนลดที่ใช้ได้";
    public const string TotalChangedRule = "ราคาเปลี่ยนแปลง กรุณาตรวจสอบยอดใหม่ก่อนยืนยัน";

    public const string PercentDiscount = "Percent";
    public const string FixedDiscount = "Fixed";

    /// <summary>Nights between two dates; the time of day is ignored.</summary>
    public static int Nights(DateTime checkIn, DateTime checkOut) => (checkOut.Date - checkIn.Date).Days;

    /// <summary>Price before any discount: (room rate for this hotel and room type) × nights × rooms.</summary>
    public static decimal Subtotal(decimal hotelPrice, decimal roomExtra, int nights, int rooms) =>
        (hotelPrice + roomExtra) * nights * rooms;

    /// <summary>A nightly rate must never be negative, or a booking could pay the customer.</summary>
    public static bool RatesValid(decimal hotelPrice, decimal roomExtra) => hotelPrice >= 0 && roomExtra >= 0;

    /// <summary>The total the customer saw must still be the total being charged.</summary>
    public static bool TotalUnchanged(decimal? expectedTotal, decimal total) =>
        expectedTotal is not null && expectedTotal == total;

    /// <summary>A promotion code written by the customer, trimmed and upper-cased; null or blank becomes null.</summary>
    public static string? NormaliseCode(string? code)
    {
        code = code?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(code) ? null : code;
    }

    /// <summary>
    /// Why this code cannot be used today, or null when it can: it must exist, be switched on, be inside its dates and
    /// be worth something (<see cref="BookingRules.PromotionOpen"/>).
    /// </summary>
    public static string? CodeClosedReason(Promotion? promotion, DateTime today) =>
        promotion is null || !BookingRules.PromotionOpen(promotion, today) ? PromotionClosedRule : null;

    /// <summary>Why this stay is too short or too cheap for the code, or null when it qualifies.</summary>
    public static string? CodeTooSmallReason(Promotion promotion, int nights, decimal subtotal) =>
        nights < promotion.MinNights || subtotal < promotion.MinSpend
            ? $"โค้ดนี้ต้องพักอย่างน้อย {promotion.MinNights} คืน และยอดก่อนลดขั้นต่ำ ฿{promotion.MinSpend:N2}"
            : null;

    /// <summary>The discount in baht, or the reason it cannot be given. Exactly one of the two is set.</summary>
    public readonly record struct DiscountResult(decimal Amount, string? Error)
    {
        public bool Ok => Error is null;
        public static DiscountResult Refused(string error) => new(0, error);
    }

    /// <summary>
    /// How much this code takes off: a percentage of the subtotal or a fixed amount, capped by the code's maximum,
    /// never more than the subtotal and never negative, rounded to satang (0.5 rounds up, the same way the money
    /// columns are rounded elsewhere). A code that works out to nothing is refused instead of booked at full price.
    /// </summary>
    public static DiscountResult Discount(Promotion promotion, decimal subtotal)
    {
        if (promotion.DiscountKind is not (PercentDiscount or FixedDiscount)) return DiscountResult.Refused(PromotionKindRule);
        var discount = promotion.DiscountKind == PercentDiscount
            ? subtotal * promotion.DiscountValue / 100
            : promotion.DiscountValue;
        if (promotion.MaxDiscount is > 0) discount = Math.Min(discount, promotion.MaxDiscount.Value);
        discount = decimal.Round(Math.Clamp(discount, 0, subtotal), 2, MidpointRounding.AwayFromZero);
        return discount <= 0 ? DiscountResult.Refused(PromotionNoDiscountRule) : new(discount, null);
    }
}
