using System.Text.RegularExpressions;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public static partial class HotelCatalogRules
{
    public static IReadOnlyList<string> LegacyAmenities { get; } =
        Array.AsReadOnly(new[] { "Wi-Fi ฟรี", "ห้องปรับอากาศ", "ห้องน้ำส่วนตัว", "พื้นที่พักผ่อน", "แผนกต้อนรับ", "ที่จอดรถ" });
    public static IReadOnlyList<string> AmenityOptions { get; } =
        Array.AsReadOnly(LegacyAmenities.Concat(new[] { "อาหารเช้า", "ร้านอาหาร", "สระว่ายน้ำ", "ฟิตเนส", "สปา", "ทางเข้าที่รองรับรถเข็น", "บริการรับส่งสนามบิน", "อนุญาตให้นำสัตว์เลี้ยงเข้าพัก" }).ToArray());

    // Booking subtotal and total are stored as DECIMAL(12,2).
    public static int MaxBasePrice(decimal highestRoomExtra) =>
        (int)Math.Max(0m, decimal.Floor(9_999_999_999.99m / (BookingRules.MaxRooms * BookingRules.MaxNights) - highestRoomExtra));

    public static HotelData NewHotel(HotelCatalogForm form)
    {
        var hotel = new HotelData { Id = Guid.NewGuid().ToString("N"), IsActive = form.IsActive };
        ApplyDetails(hotel, form);
        return hotel;
    }

    public static bool IsPhotoUrlValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var url = value.Trim();
        if (LocalHotelPhoto().IsMatch(url) || HotelPhotoService.IsUploadedUrl(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && absolute.Scheme is "https" or "http"
            && absolute.Host.Length > 0
            && absolute.UserInfo.Length == 0;
    }

    public static void ApplyDetails(HotelData hotel, HotelCatalogForm form)
    {
        hotel.Name = form.Name.Trim();
        hotel.City = form.City.Trim();
        hotel.Price = form.Price;
        hotel.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        hotel.Amenities = string.Join('\n', AmenityOptions.Where(form.Amenities.Contains));

        // NULL lets startup seed a photo. An empty URL records that an admin chose no photo.
        var photo = form.PhotoUrl?.Trim() ?? "";
        if (string.Equals(hotel.PhotoUrl, photo, StringComparison.Ordinal)) return;
        hotel.PhotoUrl = photo;
        hotel.PhotoSourceUrl = null;
        hotel.PhotoCredit = null;
    }

    [GeneratedRegex(@"\A/images/hotels/[A-Za-z0-9_-][A-Za-z0-9_.-]*\z")]
    private static partial Regex LocalHotelPhoto();
}
