using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public sealed class HotelRoomCatalog(ApplicationDbContext db)
{
    public async Task<IReadOnlyDictionary<string, decimal?>> StartingRatesAsync(IReadOnlyList<HotelData> hotels)
    {
        var shared = await db.RoomTypes.AsNoTracking().Where(room => room.IsActive)
            .Select(room => new { room.Id, room.Extra }).ToListAsync();
        var local = (await db.HotelRooms.AsNoTracking()
            .Select(room => new { room.HotelId, room.Id, room.IsActive, room.Price }).ToListAsync()).ToLookup(room => room.HotelId);
        return hotels.ToDictionary(hotel => hotel.Id, hotel =>
        {
            var overrides = local[hotel.Id].ToDictionary(room => room.Id);
            return shared.Where(room => hotel.UseSharedRooms && !overrides.ContainsKey(room.Id)).Select(room => (decimal?)(hotel.Price + room.Extra))
                .Concat(overrides.Values.Where(room => room.IsActive).Select(room => (decimal?)room.Price)).Min();
        });
    }

    public async Task<RoomOption?> FindAsync(string hotelId, string roomId)
    {
        var local = await db.HotelRooms.AsNoTracking().SingleOrDefaultAsync(room => room.HotelId == hotelId && room.Id == roomId);
        var shared = local is not null || await db.Hotels.AnyAsync(hotel => hotel.Id == hotelId && hotel.UseSharedRooms)
            ? await db.RoomTypes.AsNoTracking().SingleOrDefaultAsync(room => room.Id == roomId)
            : null;
        return HotelRoomRules.Resolve(shared, local);
    }

    public async Task<IReadOnlyList<RoomOption>> ListAsync(string hotelId, bool includeInactive = false)
    {
        var shared = await db.RoomTypes.AsNoTracking().ToDictionaryAsync(room => room.Id);
        var local = await db.HotelRooms.AsNoTracking().Where(room => room.HotelId == hotelId).ToDictionaryAsync(room => room.Id);
        var useShared = await db.Hotels.AnyAsync(hotel => hotel.Id == hotelId && hotel.UseSharedRooms);
        return (useShared ? shared.Keys.Union(local.Keys) : local.Keys)
            .Select(id => HotelRoomRules.Resolve(shared.GetValueOrDefault(id), local.GetValueOrDefault(id))!)
            .Where(room => includeInactive || room.IsActive)
            .OrderBy(room => room.Extra).ThenBy(room => room.HotelPrice).ThenBy(room => room.Id)
            .ToList();
    }
}
