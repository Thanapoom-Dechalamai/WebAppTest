using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.ViewModels;

public class ProfileForm
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ"), StringLength(80)]
    public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "กรุณากรอกนามสกุล"), StringLength(80)]
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? AvatarFile { get; set; }
    public IFormFile? Photo { get; set; }
}

public class TopUpForm
{
    [Range(typeof(decimal), "100", "50000", ErrorMessage = "เติมเครดิตได้ครั้งละ 100–50,000 บาท")]
    public decimal Amount { get; set; } = 1000;
    [Required, StringLength(36)] public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
public class WalletPage
{
    public decimal Balance { get; set; }
    public TopUpForm Form { get; set; } = new();
}

// /Member/CreditHistory: every wallet entry of the signed-in customer, newest first, one page at a time.
public record CreditHistoryPage(decimal Balance, IReadOnlyList<WalletEntry> Entries, int Page, int PageCount, int Total)
{
    public const int PageSize = 50;
}

public class BookingForm
{
    [StringLength(40)] public string? PromotionCode { get; set; }
    public decimal? ExpectedTotal { get; set; }
    [Required, StringLength(32)] public string HotelId { get; set; } = "";
    // Credit (default) or Transfer: a bank transfer with a slip and/or reference that Lodgo staff check first.
    [RegularExpression("^(Credit|Transfer)$")] public string PaymentMethod { get; set; } = "Credit";
    [StringLength(64, ErrorMessage = "เลขที่รายการยาวได้ไม่เกิน 64 ตัวอักษร")] public string? PaymentRef { get; set; }
    public IFormFile? Slip { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever] public string? SlipFile { get; set; }
    [Required, StringLength(32)] public string RoomType { get; set; } = "deluxe";
    [Required, StringLength(36)] public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CheckIn { get; set; } = DateTime.UtcNow.AddHours(7).Date.AddDays(1);
    public DateTime CheckOut { get; set; } = DateTime.UtcNow.AddHours(7).Date.AddDays(2);
    [Range(1, 5)] public int Rooms { get; set; } = 1;
    [Range(1, 10)] public int Guests { get; set; } = 2;
    [StringLength(300, ErrorMessage = "คำขอพิเศษยาวได้ไม่เกิน 300 ตัวอักษร")] public string? SpecialRequest { get; set; }
}
public record RoomOption(string Id, string Name, decimal Extra, string Image, string Features)
{
    public decimal? HotelPrice { get; init; }
    public int Capacity { get; init; } = RoomCapacity.Default(Id);
    public string BookingName { get; init; } = Name;
    public bool IsActive { get; init; } = true;
    public int? SizeSquareMeters { get; init; }
    public string? BedType { get; init; }
    public int? BedCount { get; init; }
    public decimal NightlyRate(int basePrice) => HotelPrice ?? basePrice + Extra;
}
public static class RoomOptions
{
    public static IReadOnlyList<RoomOption> All { get; } = [
        new("deluxe", "ดีลักซ์ · เตียงใหญ่", 0, "garden", "32 ตร.ม. · เตียงใหญ่ 1 เตียง · Wi-Fi ฟรี"),
        new("twin", "พรีเมียร์ · เตียงแฝด", 450, "river", "38 ตร.ม. · เตียงเดี่ยว 2 เตียง · รวมอาหารเช้า"),
        new("suite", "สวีท · พื้นที่พักผ่อน", 1100, "samui", "52 ตร.ม. · เตียงใหญ่และโซฟา · รวมอาหารเช้า")
    ];
}
public record HotelPage(HotelCardViewModel Hotel, IReadOnlyList<RoomOption> Rooms)
{
    public IReadOnlyList<HotelGalleryPhoto> Photos { get; init; } = [];
    public ReviewSection? Reviews { get; init; }
    // For a signed-in customer: can they write a review for this hotel now, and if not, why
    public ReviewInvite? Invite { get; init; }
}
public record ReviewInvite(string State, int? OrderId = null)
{
    public const string Ready = "ready", Upcoming = "upcoming", Reviewed = "reviewed", None = "none";
}
public record CheckoutPage(HotelCardViewModel Hotel, RoomOption Room, BookingForm Form, decimal Balance, decimal Total)
{
 public decimal Subtotal {get;init;}
 public decimal Discount {get;init;}
 public string? AppliedCode {get;init;}
}
