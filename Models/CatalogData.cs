namespace Nontiwat_7362_L002.Models;
public class HotelData
{
    public bool UseSharedRooms { get; set; } = true;
 public string Id { get; set; } = "";
 public string Name { get; set; } = "";
 public string City { get; set; } = "";
 public string ImageKey { get; set; } = "";
 public string SourceUrl { get; set; } = "";
 public int Price { get; set; }
 public string? PhotoUrl { get; set; }
 public string? PhotoSourceUrl { get; set; }
 public string? PhotoCredit { get; set; }
 public string? Description { get; set; }
 public string? Amenities { get; set; }
 public bool IsActive { get; set; } = true;
 // Share of each booking Lodgo keeps (0-30). Copied into the booking when it is made.
 public decimal CommissionPercent { get; set; } = 10m;
}
public class RoomTypeData
{
 public string Id { get; set; } = "";
 public string Name { get; set; } = "";
 public decimal Extra { get; set; }
 public string Image { get; set; } = "";
 public string Features { get; set; } = "";
 public bool IsActive { get; set; } = true;
}
