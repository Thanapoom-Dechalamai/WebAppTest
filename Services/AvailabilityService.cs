using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Services;

// Room calendar: how many rooms of each type a hotel sells per night, and how many are taken.
// Booking checks it so a hotel can never be booked past the rooms it has.
public class AvailabilityService(ApplicationDbContext db, IThaiClock clock, HotelRoomCatalog rooms) : IBookingAvailability
{
    Task IBookingAvailability.EnsureAvailableAsync(string hotelId, string roomType, string roomName,
        DateTime checkIn, DateTime checkOut, int rooms) =>
        EnsureAvailableAsync(hotelId, roomType, roomName, checkIn, checkOut, rooms);

    public record Night(DateTime Date, int Capacity, int Booked, bool Closed)
    {
        public int Left => Closed ? 0 : Math.Max(0, Capacity - Booked);
    }

    // Nights from 'from' up to, but not including, 'to' (the check-out day is not a night).
    // roomName also matches bookings saved before the RoomType column was filled in.
    public async Task<IReadOnlyList<Night>> NightsAsync(string hotelId, string roomType, string roomName, DateTime from, DateTime to)
    {
        from = from.Date; to = to.Date;
        var room = await rooms.FindAsync(hotelId, roomType);
        var bookingName = room?.BookingName ?? roomName;
        var capacity = room?.Capacity ?? RoomCapacity.Default(roomType);
        var set = await db.RoomAllotments.AsNoTracking()
            .Where(x => x.HotelId == hotelId && x.RoomType == roomType && x.Date >= from && x.Date < to)
            .ToDictionaryAsync(x => x.Date.Date);
        var holding = RoomCapacity.HoldingStatuses;
        var stays = await db.Orders.AsNoTracking()
            .Where(x => x.HotelId == hotelId && (x.RoomType == roomType || (x.RoomType == null && x.RoomName == bookingName))
                && holding.Contains(x.Status) && x.CheckIn < to && x.CheckOut > from)
            .Select(x => new { x.CheckIn, x.CheckOut, x.Rooms }).ToListAsync();
        var nights = new List<Night>();
        for (var day = from; day < to; day = day.AddDays(1))
        {
            var booked = stays.Where(s => s.CheckIn.Date <= day && s.CheckOut.Date > day).Sum(s => s.Rooms);
            set.TryGetValue(day, out var row);
            var nightlyCapacity = row is { HasCapacityOverride: true } ? row.Rooms : capacity;
            nights.Add(new Night(day, nightlyCapacity, booked, room is { IsActive: false } || (row?.Closed ?? false)));
        }
        return nights;
    }

    // Throws when a night of the stay cannot take 'rooms' more rooms. 'alsoTaken' holds rooms
    // claimed earlier in the same checkout (several Cart items for the same room type).
    public async Task EnsureAvailableAsync(string hotelId, string roomType, string roomName, DateTime checkIn, DateTime checkOut,
        int rooms, IReadOnlyDictionary<DateTime, int>? alsoTaken = null)
    {
        foreach (var night in await NightsAsync(hotelId, roomType, roomName, checkIn, checkOut))
        {
            if (night.Closed)
                throw new ArgumentException($"โรงแรมปิดรับจอง{roomName} คืน{ThaiDates.Long(night.Date)} กรุณาเลือกวันหรือประเภทห้องอื่น");
            var left = night.Left - (alsoTaken is not null && alsoTaken.TryGetValue(night.Date, out var taken) ? taken : 0);
            if (left < rooms)
                throw new ArgumentException(left <= 0
                    ? $"{roomName} เต็มแล้วคืน{ThaiDates.Long(night.Date)} กรุณาเลือกวันหรือประเภทห้องอื่น"
                    : $"{roomName} เหลือ {left} ห้องคืน{ThaiDates.Long(night.Date)} กรุณาลดจำนวนห้อง");
        }
    }

    public async Task<IReadOnlyList<CalendarRow>> MonthAsync(string hotelId, DateTime month)
    {
        var from = new DateTime(month.Year, month.Month, 1);
        var rows = new List<CalendarRow>();
        foreach (var room in await rooms.ListAsync(hotelId, includeInactive: true))
            rows.Add(new CalendarRow(room.Id, room.Name, await NightsAsync(hotelId, room.Id, room.Name, from, from.AddMonths(1))));
        return rows;
    }

    // Admin manages every hotel; HotelStaff only the hotels whose team they are on.
    public async Task<bool> CanManageAsync(int actorId, string hotelId)
    {
        var role = await db.Users.AsNoTracking().Where(x => x.Id == actorId).Select(x => x.Role).SingleOrDefaultAsync();
        if (role == AppRoles.Admin) return true;
        return role == AppRoles.HotelOwner && await db.HotelAssignments.AnyAsync(x => x.HotelId == hotelId && x.OwnerUserId == actorId);
    }

    // Changes every night from form.From to form.To (both included). Returns the number of nights changed.
    public async Task<int> SetAsync(int actorId, AllotmentForm form)
    {
        if (!await CanManageAsync(actorId, form.HotelId)) throw new UnauthorizedAccessException("ไม่มีสิทธิ์จัดการโรงแรมนี้");
        if (form.Action is not ("rooms" or "close" or "open")) throw new ArgumentException("เลือกสิ่งที่ต้องการเปลี่ยน");
        var from = form.From.Date;
        var to = form.To.Date;
        if (from < clock.Today) throw new ArgumentException("แก้ได้ตั้งแต่คืนนี้เป็นต้นไป");
        if (to < from || (to - from).Days > 365) throw new ArgumentException("วันสิ้นสุดต้องไม่ก่อนวันเริ่ม และช่วงวันไม่เกิน 1 ปี");
        if (form.Action == "rooms" && form.Rooms is not (>= 0 and <= RoomCapacity.Max))
            throw new ArgumentException($"จำนวนห้องต้องอยู่ระหว่าง 0–{RoomCapacity.Max}");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Same row lock as a booking, so nobody books while the numbers change.
        var hotel = (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {form.HotelId} FOR UPDATE").AsNoTracking().ToListAsync()).SingleOrDefault()
            ?? throw new ArgumentException("ไม่พบโรงแรม");
        var room = await rooms.FindAsync(hotel.Id, form.RoomType) ?? throw new ArgumentException("ไม่พบประเภทห้อง");
        var end = to.AddDays(1);
        var nights = await NightsAsync(hotel.Id, room.Id, room.Name, from, end);
        if (form.Action == "rooms")
        {
            var tooFew = nights.FirstOrDefault(n => n.Booked > form.Rooms!.Value);
            if (tooFew is not null)
                throw new ArgumentException($"คืน{ThaiDates.Long(tooFew.Date)} มีการจองแล้ว {tooFew.Booked} ห้อง ตั้งจำนวนห้องน้อยกว่านี้ไม่ได้");
        }
        var rows = await db.RoomAllotments.Where(x => x.HotelId == hotel.Id && x.RoomType == room.Id && x.Date >= from && x.Date < end)
            .ToDictionaryAsync(x => x.Date.Date);
        foreach (var night in nights)
        {
            if (!rows.TryGetValue(night.Date, out var row))
            {
                row = new RoomAllotment { HotelId = hotel.Id, RoomType = room.Id, Date = night.Date, HasCapacityOverride = false };
                db.RoomAllotments.Add(row);
            }
            if (form.Action == "rooms") { row.Rooms = form.Rooms!.Value; row.HasCapacityOverride = true; }
            else row.Closed = form.Action == "close";
            row.UpdatedAtUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return nights.Count;
    }
}
