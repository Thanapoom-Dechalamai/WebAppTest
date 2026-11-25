using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

// Hotels a customer liked with the heart button.
public class WishlistService(ApplicationDbContext db, CatalogService catalog)
{
    // Returns true when the hotel is now liked. Unknown hotel → KeyNotFoundException.
    // INSERT IGNORE / DELETE keep a fast double click from failing on the primary key.
    public async Task<bool> ToggleAsync(int userId, string hotelId)
    {
        if (await catalog.FindAsync(hotelId) is null) throw new KeyNotFoundException(hotelId);
        var removed = await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM wishlists WHERE UserId = {userId} AND HotelId = {hotelId}");
        if (removed > 0) return false;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT IGNORE INTO wishlists (UserId, HotelId, CreatedAtUtc) VALUES ({userId}, {hotelId}, UTC_TIMESTAMP(6))");
        return true;
    }

    public async Task<HashSet<string>> IdsAsync(int userId) =>
        (await db.Wishlists.AsNoTracking().Where(x => x.UserId == userId).Select(x => x.HotelId).ToListAsync()).ToHashSet();

    public Task<int> CountAsync(int userId) => db.Wishlists.CountAsync(x => x.UserId == userId);

    // Newest like first.
    public async Task<IReadOnlyList<HotelCardViewModel>> HotelsAsync(int userId)
    {
        var ids = await db.Wishlists.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => x.HotelId).ToListAsync();
        var all = (await catalog.AllAsync()).ToDictionary(x => x.Id);
        return ids.Where(all.ContainsKey).Select(id => all[id]).ToList();
    }
}
