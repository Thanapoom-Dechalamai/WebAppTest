namespace Nontiwat_7362_L002.Models;

// A room a customer wants to book later. Prices are not stored: the cart always shows
// today's price, and checkout confirms the total the customer saw before paying.
public class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    // Becomes the order's RequestId at checkout, so paying twice cannot create the order twice.
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string HotelId { get; set; } = "";
    public string RoomType { get; set; } = "";
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Rooms { get; set; }
    public int Guests { get; set; }
    public string? SpecialRequest { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
