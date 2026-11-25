using System.Globalization;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels;

// "My trips": bookings split into upcoming / finished / cancelled tabs.
public static class BookingTabs
{
    public const string Upcoming = "upcoming";
    public const string Finished = "finished";
    public const string Cancelled = "cancelled";
    public static string Normalize(string? tab) => tab is Finished or Cancelled ? tab : Upcoming;
    public static string[] Statuses(string tab) => tab switch
    {
        Finished => [BookingStatus.Completed, BookingStatus.NoShow],
        Cancelled => [BookingStatus.Cancelled],
        _ => [BookingStatus.PaymentReview, BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.CheckedIn]
    };
}

public record BookingListPage(IReadOnlyList<StayOrder> Orders, IReadOnlyDictionary<string, HotelCardViewModel> Hotels,
    string Tab, string? Query, string Sort, int UpcomingCount, int FinishedCount, int CancelledCount)
{
    public IReadOnlySet<int> ReviewedOrderIds { get; init; } = new HashSet<int>();
}

public record BookingDetailPage(StayOrder Order, HotelCardViewModel? Hotel, RoomOption? Room,
    string GuestName, string GuestEmail, string? DecisionNote, int HotelTeamSize)
{
    public int Nights => (Order.CheckOut - Order.CheckIn).Days;
    public bool Reviewed { get; init; }
    public string? DemoStep { get; init; }
    // Who did what, oldest first.
    public IReadOnlyList<TimelineItem> Timeline { get; init; } = [];
    public bool CanReview => Order.Status == BookingStatus.Completed && !Reviewed;
}

public static class ThaiDates
{
    // Thai day/month names with Gregorian years, matching the rest of the site.
    private static readonly CultureInfo Thai = CreateThai();
    private static CultureInfo CreateThai()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("th-TH").Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        return culture;
    }
    public static string Long(DateTime date) => date.ToString("ddd d MMM yyyy", Thai);
    public static string Short(DateTime date) => date.ToString("ddd d MMM", Thai);
    public static string MonthYear(DateTime date) => date.ToString("MMMM yyyy", Thai);
    public static string DayName(DateTime date) => date.ToString("ddd", Thai);
}


public record ReviewQuery(int? Stars, string? Type, string Sort, int Page);
public record ReviewSection(int Total, double Average, IReadOnlyDictionary<int, int> StarCounts,
    IReadOnlyDictionary<string, double> AspectAverages, IReadOnlyDictionary<string, int> TypeCounts,
    IReadOnlyList<HotelReview> Items, int MatchCount, int Page, int PageCount, ReviewQuery Query);
public class ReviewForm
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] public int OrderId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "ให้คะแนนภาพรวม 1–5 ดาว")] public int Rating { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "ให้คะแนนความสะอาด 1–5 ดาว")] public int Cleanliness { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "ให้คะแนนทำเล 1–5 ดาว")] public int Location { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "ให้คะแนนการบริการ 1–5 ดาว")] public int Service { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, 5, ErrorMessage = "ให้คะแนนความคุ้มค่า 1–5 ดาว")] public int Value { get; set; }
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "เลือกประเภทผู้เข้าพัก")] public string TravelerType { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "ใส่หัวข้อรีวิว"), System.ComponentModel.DataAnnotations.StringLength(120, MinimumLength = 3, ErrorMessage = "หัวข้อ 3–120 ตัวอักษร")] public string Title { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "เล่าประสบการณ์การเข้าพัก"), System.ComponentModel.DataAnnotations.StringLength(1500, MinimumLength = 20, ErrorMessage = "รีวิว 20–1,500 ตัวอักษร")] public string Body { get; set; } = "";
}
public record ReviewFormPage(StayOrder Order, HotelCardViewModel? Hotel, ReviewForm Form);
