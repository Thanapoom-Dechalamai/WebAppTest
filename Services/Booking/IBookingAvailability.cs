namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>Checks room capacity for a quote and again under the hotel row lock at checkout.</summary>
public interface IBookingAvailability
{
    Task EnsureAvailableAsync(string hotelId, string roomType, string roomName, DateTime checkIn, DateTime checkOut, int rooms);
}
