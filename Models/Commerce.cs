namespace Nontiwat_7362_L002.Models;

public class MemberWallet
{
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public string? AvatarFile { get; set; }
}

public class StayOrder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequestId { get; set; } = "";
    public string HotelId { get; set; } = "";
    public string HotelName { get; set; } = "";
    public string City { get; set; } = "";
    public string RoomName { get; set; } = "";
    // Room type id (deluxe / twin / suite). Used to count rooms taken per night.
    public string? RoomType { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Rooms { get; set; }
    public int Guests { get; set; }
    public decimal NightlyRate { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? PromotionCode { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = BookingStatus.Pending;
    // Optional note from the guest (late check-in, high floor...). The hotel tries but does not promise.
    public string? SpecialRequest { get; set; }
    // Credit (paid from the wallet at once) or Transfer (bank transfer, Staff checks the slip/reference first).
    public string PaymentMethod { get; set; } = PaymentMethods.Credit;
    public string? PaymentRef { get; set; }
    public string? SlipFile { get; set; }
    // Lodgo's share, locked when the booking is made (see FinanceRules.Commission).
    public decimal CommissionPercent { get; set; } = FinanceRules.CommissionPercent;
    // When the hotel got the request (null on rows from before 2026-11-22: use CreatedAtUtc). Hotel response times start here.
    public DateTime? HotelSentAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class PaymentMethods
{
    public const string Credit = "Credit";
    public const string Transfer = "Transfer";
}

public class HotelAccessRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string HotelId { get; set; } = "";
    public string? Message { get; set; }
    public string ProofFile { get; set; } = "";
    public string Status { get; set; } = AccessRequestStatus.Pending;
    public int? ReviewerId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
}

public static class AccessRequestStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public static string Label(string s) => s switch { Pending => "รอพิจารณา", Approved => "อนุมัติแล้ว", Rejected => "ไม่อนุมัติ", _ => s };
}

public class WalletEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequestId { get; set; } = "";
    public string Kind { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
