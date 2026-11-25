namespace Nontiwat_7362_L002.Models;

// One line in a booking's history: who did what and when. Written for every status change,
// so the customer and the hotel can both see where a booking is and who is handling it.
public class BookingEvent
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    // Null when the system did it (for example an unanswered request expiring).
    public int? ActorUserId { get; set; }
    // Kept as text so the history still reads correctly after the person changes role or leaves.
    public string ActorLabel { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// In-site message for one account (customer or hotel staff). Shown under the bell in the navbar.
public class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int? OrderId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// Rooms a hotel sells for one room type on one night. No row means the default from RoomCapacity.
public class RoomAllotment
{
    public string HotelId { get; set; } = "";
    public string RoomType { get; set; } = "";
    public DateTime Date { get; set; }
    public int Rooms { get; set; }
    public bool HasCapacityOverride { get; set; } = true;
    public bool Closed { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class RoomCapacity
{
    // Rooms per night until the hotel sets its own numbers in the calendar.
    public static int Default(string roomType) => roomType switch { "deluxe" => 8, "twin" => 6, "suite" => 3, _ => 5 };
    public const int Max = 500;
    // Bookings that hold a room on their nights. Cancelled / no-show / completed stays free it.
    public static readonly string[] HoldingStatuses = [BookingStatus.PaymentReview, BookingStatus.Pending, BookingStatus.Confirmed, BookingStatus.CheckedIn];
}
