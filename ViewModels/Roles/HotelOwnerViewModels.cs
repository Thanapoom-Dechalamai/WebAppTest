using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels.Roles;
public record HotelOwnerOrder(StayOrder Order, string CustomerName, string Email, string? Note);
public record HotelWorkQueue(int ToConfirm, int Arriving, int InHouse);
public record HotelOwnerPage(IReadOnlyList<HotelCardViewModel> Hotels, IReadOnlyList<HotelOwnerOrder> Orders,
    IReadOnlyList<HotelOwnerEntry> Entries, decimal Receipts, decimal Refunds, decimal Pending, string? Status, bool IsAdmin)
{
    public HotelWorkQueue Queue { get; init; } = new(0, 0, 0);
    // Finished stays minus Lodgo commission, and what Lodgo has already transferred.
    public decimal Owed { get; init; }
    public decimal PaidOut { get; init; }
    // Dashboard (2026-11-22): booking summary donut, the week strip and who arrives/leaves on the chosen day.
    public BookingSummary Summary { get; init; } = new("week", 0, 0, 0);
    public DateTime Day { get; init; }
    public HotelCardViewModel? CalendarHotel { get; init; }
    public IReadOnlyList<RoomDay> RoomDays { get; init; } = [];
}
public record BookingSummary(string Range, int Confirmed, int Pending, int Cancelled)
{
    public int Total => Confirmed + Pending + Cancelled;
    public int Pct(int n) => Total == 0 ? 0 : (int)Math.Round(n * 100.0 / Total);
    public string RangeLabel => Range switch { "month" => "เดือนนี้", "30" => "30 วันล่าสุด", _ => "7 วันล่าสุด" };
}
// Kind: in = arrives (confirmed), wait = arrives but not confirmed yet, out = leaves, stay = in the hotel.
public record DayStay(HotelOwnerOrder Row, string Kind);
public record RoomDay(string RoomType, string RoomName, int Left, int Capacity, IReadOnlyList<DayStay> Stays);
public record HotelManagementPage(IReadOnlyList<HotelCardViewModel> Hotels, IReadOnlyList<UserRowViewModel> Owners, IReadOnlyList<HotelAssignment> Assignments)
{
    // Lodgo's share per hotel (hotels.CommissionPercent).
    public IReadOnlyDictionary<string, decimal> Rates { get; init; } = new Dictionary<string, decimal>();
}
public class HotelRejectForm
{
    [Range(1,int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(120)] public string Reason { get; set; } = "";
}
public class AssignHotelForm
{
    [Required,StringLength(32)] public string HotelId { get; set; } = "";
    [Range(1,int.MaxValue)] public int OwnerUserId { get; set; }
}

// "แขกวันนี้" page: hotel picker + the day's board.
public record TodayGuestsPage(IReadOnlyList<Nontiwat_7362_L002.ViewModels.HotelCardViewModel> Hotels, Nontiwat_7362_L002.ViewModels.HotelCardViewModel? Hotel, Nontiwat_7362_L002.ViewModels.GuestBoard Board);
public record HotelAccessPage(IReadOnlyList<HotelCardViewModel> Hotels, IReadOnlyList<Nontiwat_7362_L002.Services.HotelAccessService.Row> Requests);
