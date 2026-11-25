namespace Nontiwat_7362_L002.Models;
public class HotelAssignment
{
    public string HotelId { get; set; } = "";
    public int OwnerUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
public class HotelOwnerEntry
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public int OrderId { get; set; }
    public string HotelId { get; set; } = "";
    public string HotelName { get; set; } = "";
    public string Kind { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
// Money Lodgo has transferred to a hotel for stays that are finished (Completed or NoShow).
public class HotelPayout
{
    public int Id { get; set; }
    public string HotelId { get; set; } = "";
    public string HotelName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reference { get; set; } = "";
    // How the money left Lodgo: BankTransfer, PromptPay or Cheque (see FinanceRules.PayoutMethods).
    public string Method { get; set; } = "BankTransfer";
    public string? Note { get; set; }
    public int PaidByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
public static class FinanceRules
{
    // Default share Lodgo keeps; each hotel can have its own (hotels.CommissionPercent, 0-30),
    // and the rate is copied into every booking when it is made so later changes never rewrite history.
    public const decimal CommissionPercent = 10m;
    public const decimal MaxCommissionPercent = 30m;
    public static decimal Commission(decimal total, decimal percent) =>
        decimal.Round(total * percent / 100m, 2, MidpointRounding.AwayFromZero);
    public static decimal Commission(StayOrder order) => Commission(order.Total, order.CommissionPercent);
    public static decimal HotelShare(StayOrder order) => order.Total - Commission(order);
    // Commission is worked out per booking, so a hotel's total always equals the sum of its bookings.
    public static decimal CommissionOf(IEnumerable<(decimal Total, decimal Percent)> bookings) => bookings.Sum(b => Commission(b.Total, b.Percent));
    public static readonly IReadOnlyDictionary<string, string> PayoutMethods = new Dictionary<string, string>
    {
        ["BankTransfer"] = "โอนผ่านบัญชีธนาคาร", ["PromptPay"] = "พร้อมเพย์", ["Cheque"] = "เช็ค"
    };
    public static string MethodLabel(string? method) => method is not null && PayoutMethods.TryGetValue(method, out var label) ? label : "ไม่ระบุ";
    // The hotel earns its share once the guest has arrived (or did not come): nothing is refunded after that.
    public static readonly string[] EarnedStatuses = [BookingStatus.CheckedIn, BookingStatus.Completed, BookingStatus.NoShow];
    public static readonly string[] DeferredStatuses = [BookingStatus.Confirmed];
}
public class HotelDecision
{
    public int OrderId { get; set; }
    public int ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
public static class BookingStatus
{
    public const string CheckedIn = "CheckedIn";
    public const string Completed = "Completed";
    public const string Pending = "Pending";
    // Paid by bank transfer: Lodgo staff check the slip/reference before the hotel sees the booking.
    public const string PaymentReview = "PaymentReview";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";
    public static string Label(string status) => status switch { CheckedIn => "กำลังเข้าพัก", Completed => "เข้าพักเสร็จสิ้น", Pending => "รอพนักงานโรงแรมยืนยัน", PaymentReview => "รอตรวจการชำระเงิน", Confirmed => "ยืนยันการจองแล้ว", Cancelled => "ยกเลิกและคืนเครดิตแล้ว", NoShow => "ลูกค้าไม่มาเข้าพัก", _ => status };
    public static readonly string[] All = [PaymentReview, Pending, Confirmed, CheckedIn, Completed, NoShow, Cancelled];
}
