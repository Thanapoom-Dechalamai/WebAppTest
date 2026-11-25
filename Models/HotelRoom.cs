namespace Nontiwat_7362_L002.Models;

public sealed class HotelRoom
{
    public string HotelId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int? SizeSquareMeters { get; set; }
    public string? BedType { get; set; }
    public int? BedCount { get; set; }
    public string? Features { get; set; }
    public bool IsActive { get; set; }
}

public sealed class HotelGalleryPhoto
{
    public int Id { get; set; }
    public string HotelId { get; set; } = "";
    public string? RoomId { get; set; }
    public string Url { get; set; } = "";
    public string? Credit { get; set; }
}
