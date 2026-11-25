using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public sealed class HotelRoomAdminService(ApplicationDbContext db, HotelRoomCatalog catalog, AvailabilityService availability, IThaiClock clock)
{
    public async Task<bool> SaveAsync(string hotelId, string? roomId, HotelRoomForm form)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var hotel = (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {hotelId} FOR UPDATE")
            .AsNoTracking().ToListAsync()).SingleOrDefault();
        if (hotel is null || await StageAsync(hotel, roomId, form) is null) return false;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    // The caller owns the transaction and the hotel lock when editing an existing hotel.
    internal async Task<string?> StageAsync(HotelData hotel, string? roomId, HotelRoomForm form, bool preserveShared = false)
    {
        var current = roomId is null ? null : await catalog.FindAsync(hotel.Id, roomId);
        if (roomId is not null && current is null) return null;
        if (preserveShared && current is { HotelPrice: null } &&
            form.Name.Trim() == current.Name && form.Price == current.NightlyRate(hotel.Price) &&
            form.Capacity == current.Capacity && form.BedCount == current.BedCount &&
            form.SizeSquareMeters == current.SizeSquareMeters &&
            string.Equals(form.BedType?.Trim() ?? "", current.BedType ?? "", StringComparison.Ordinal) &&
            string.Equals(form.Features?.Trim() ?? "", current.Features, StringComparison.Ordinal) && form.IsActive == current.IsActive)
            return current.Id;
        if (current is not null && form.Capacity < current.Capacity)
            await CheckExistingBookingsAsync(hotel.Id, current, form.Capacity);

        var id = current?.Id ?? Guid.NewGuid().ToString("N");
        var room = await db.HotelRooms.FindAsync(hotel.Id, id);
        if (room is null)
        {
            room = new HotelRoom { HotelId = hotel.Id, Id = id };
            db.HotelRooms.Add(room);
        }
        room.Name = form.Name.Trim();
        room.Price = form.Price;
        room.Capacity = form.Capacity;
        room.SizeSquareMeters = form.SizeSquareMeters;
        room.BedType = form.BedType?.Trim();
        room.BedCount = form.BedCount;
        room.Features = form.Features?.Trim();
        room.IsActive = form.IsActive;
        return id;
    }

    private async Task CheckExistingBookingsAsync(string hotelId, RoomOption room, int capacity)
    {
        var from = clock.Today;
        var lastCheckout = await db.Orders.Where(order => order.HotelId == hotelId
                && (order.RoomType == room.Id || (order.RoomType == null && order.RoomName == room.BookingName))
                && RoomCapacity.HoldingStatuses.Contains(order.Status) && order.CheckOut > from)
            .MaxAsync(order => (DateTime?)order.CheckOut);
        if (lastCheckout is null) return;

        var overrides = (await db.RoomAllotments.Where(row => row.HotelId == hotelId && row.RoomType == room.Id && row.HasCapacityOverride
                    && row.Date >= from && row.Date < lastCheckout.Value)
                .Select(row => row.Date).ToListAsync()).ToHashSet();
        var nights = await availability.NightsAsync(hotelId, room.Id, room.Name, from, lastCheckout.Value);
        var conflict = nights.FirstOrDefault(night => night.Booked > capacity && !overrides.Contains(night.Date));
        if (conflict is not null)
            throw new ArgumentException($"คืน {ThaiDates.Long(conflict.Date)} มีการจอง {conflict.Booked} ห้อง กรุณาตั้งจำนวนห้องให้ครอบคลุมรายการเดิม");
    }
}
