using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public static class HotelRoomRules
{
    public const decimal MaxRate = 9_999_999_999.99m / (BookingRules.MaxNights * BookingRules.MaxRooms);
    public static bool RateValid(decimal price) => price >= 1m && price <= MaxRate && decimal.Round(price, 2) == price;

    public static RoomOption? Resolve(RoomTypeData? shared, HotelRoom? local)
    {
        if (local is not null)
            return new(local.Id, local.Name, 0, shared?.Image ?? "", local.Features ?? "")
            {
                HotelPrice = local.Price, Capacity = local.Capacity, BookingName = shared?.Name ?? local.Name,
                IsActive = local.IsActive, BedType = local.BedType, BedCount = local.BedCount, SizeSquareMeters = local.SizeSquareMeters
            };
        return shared is null ? null : new(shared.Id, shared.Name, shared.Extra, shared.Image, shared.Features)
        {
            IsActive = shared.IsActive
        };
    }
}
