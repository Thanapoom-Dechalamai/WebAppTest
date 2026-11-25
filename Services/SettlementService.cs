using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Services;

// What Lodgo owes each hotel. Shared by the AC pages and the "today" panel.
public class SettlementService(ApplicationDbContext db)
{
    // Earned = the guest arrived (CheckedIn/Completed) or did not come (NoShow); Lodgo keeps the commission and owes the rest to the hotel.
    public async Task<List<SettlementRow>> RowsAsync(string? hotelId)
    {
        var orders = db.Orders.AsNoTracking().Where(x => hotelId == null || x.HotelId == hotelId);
        // Per-booking totals so commission is rounded per booking, the same way the statement shows it.
        var earned = (await orders.Where(x => FinanceRules.EarnedStatuses.Contains(x.Status))
            .Select(x => new { x.HotelId, x.Total, x.CommissionPercent }).ToListAsync())
            .GroupBy(x => x.HotelId).Select(g => new { HotelId = g.Key, Total = g.Sum(x => x.Total), Commission = FinanceRules.CommissionOf(g.Select(x => (x.Total, x.CommissionPercent))) }).ToList();
        var upcoming = await orders.Where(x => FinanceRules.DeferredStatuses.Contains(x.Status))
            .GroupBy(x => x.HotelId).Select(g => new { HotelId = g.Key, Total = g.Sum(x => x.Total) }).ToListAsync();
        var paid = await db.HotelPayouts.AsNoTracking().Where(x => hotelId == null || x.HotelId == hotelId)
            .GroupBy(x => x.HotelId).Select(g => new { HotelId = g.Key, Total = g.Sum(x => x.Amount) }).ToListAsync();
        var ids = earned.Select(x => x.HotelId).Concat(upcoming.Select(x => x.HotelId)).Concat(paid.Select(x => x.HotelId)).Distinct().ToList();
        var names = await db.Hotels.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
        return ids.Select(id => new SettlementRow(id, names.GetValueOrDefault(id, id),
                upcoming.Where(x => x.HotelId == id).Sum(x => x.Total),
                earned.Where(x => x.HotelId == id).Sum(x => x.Total),
                earned.Where(x => x.HotelId == id).Sum(x => x.Commission),
                paid.Where(x => x.HotelId == id).Sum(x => x.Total)))
            .OrderByDescending(x => x.Outstanding).ThenBy(x => x.HotelName).ToList();
    }
}
