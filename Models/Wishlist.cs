namespace Nontiwat_7362_L002.Models;

// A hotel a customer marked with the heart button. One row per customer and hotel.
public class WishlistItem
{
    public int UserId { get; set; }
    public string HotelId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
