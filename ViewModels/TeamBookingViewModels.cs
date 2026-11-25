using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.ViewModels;

public sealed class TeamBookingEdit
{
    public int OrderId { get; set; }
    [Range(1, 10)] public int Guests { get; set; }
    [StringLength(300)] public string? SpecialRequest { get; set; }
    [StringLength(64)] public string? PaymentRef { get; set; }
    public IFormFile? Slip { get; set; }
}

public sealed record TeamBookingPage(SupportCase Case, string CustomerName, StayOrder? Order, BookingForm Booking,
    IReadOnlyList<HotelCardViewModel> Hotels, IReadOnlyList<RoomOption> Rooms, bool CanChange, bool CanChangeEvidence)
{
    public IReadOnlyList<SupportQuote> Quotes { get; init; } = []; // newest first
}
