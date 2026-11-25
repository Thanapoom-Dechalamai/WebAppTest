using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels.Roles;

namespace Nontiwat_7362_L002.Services;

// Owner dashboard: who sells the most, and what needs attention.
public class OwnerInsights(ApplicationDbContext db, IThaiClock clock)
{
    public const double CancelRateLimit = 30;     // percent, design 4.7
    public const int CancelMinBookings = 5;
    public const double LowRating = 3.5;
    public const int WarningWindowDays = 90;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    // Value of bookings that were not cancelled, created in the last 12 Thai months.
    private IQueryable<StayOrder> Kept()
    {
        var since = ThaiMonth.StartUtc(clock.ThisMonth.AddMonths(-11));
        return db.Orders.AsNoTracking().Where(x => x.CreatedAtUtc >= since && x.Status != BookingStatus.Cancelled);
    }

    public async Task<IReadOnlyList<RankRow>> TopHotelsAsync(int take) =>
        (await Kept().GroupBy(x => new { x.HotelId, x.HotelName, x.City })
            .Select(g => new { g.Key.HotelId, g.Key.HotelName, g.Key.City, Amount = g.Sum(x => x.Total), Count = g.Count() })
            .OrderByDescending(x => x.Amount).Take(take).ToListAsync())
        .Select(x => new RankRow(x.HotelId, $"{x.HotelName} · {x.City}", x.Amount, x.Count)).ToList();

    public async Task<IReadOnlyList<RankRow>> TopProvincesAsync(int take) =>
        (await Kept().GroupBy(x => x.City)
            .Select(g => new { City = g.Key, Amount = g.Sum(x => x.Total), Count = g.Count() })
            .OrderByDescending(x => x.Amount).Take(take).ToListAsync())
        .Select(x => new RankRow(x.City, x.City, x.Amount, x.Count)).ToList();

    // The Owner page and its "today" tile both need the warnings in the same request: work them out once.
    private IReadOnlyList<OwnerWarning>? warnings;

    public async Task<IReadOnlyList<OwnerWarning>> WarningsAsync()
    {
        if (warnings is not null) return warnings;
        var list = new List<OwnerWarning>();
        var since = DateTime.UtcNow.AddDays(-WarningWindowDays);
        var cancels = await db.Orders.AsNoTracking().Where(x => x.CreatedAtUtc >= since && (x.PaymentMethod == PaymentMethods.Credit || x.HotelSentAtUtc != null))
            .GroupBy(x => new { x.HotelId, x.HotelName })
            .Select(g => new { g.Key.HotelId, g.Key.HotelName, Total = g.Count(), Cancelled = g.Count(x => x.Status == BookingStatus.Cancelled) })
            .Where(x => x.Total >= CancelMinBookings).ToListAsync();
        foreach (var h in cancels.Where(x => x.Cancelled * 100.0 / x.Total > CancelRateLimit).OrderByDescending(x => x.Cancelled * 1.0 / x.Total))
            list.Add(new OwnerWarning("cancel", $"{h.HotelName}: ยกเลิก {h.Cancelled} จาก {h.Total} การจอง ({h.Cancelled * 100.0 / h.Total:0}%) ใน {WarningWindowDays} วัน",
                $"/Stays/Details/{h.HotelId}"));
        var ratings = await db.HotelReviews.AsNoTracking().Where(x => !x.IsSample)
            .GroupBy(x => x.HotelId).Select(g => new { HotelId = g.Key, Avg = g.Average(x => (double)x.Rating), Count = g.Count() })
            .Where(x => x.Avg < LowRating).ToListAsync();
        if (ratings.Count > 0)
        {
            var ids = ratings.Select(x => x.HotelId).ToList();
            var names = await db.Hotels.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name);
            foreach (var r in ratings.OrderBy(x => x.Avg))
                list.Add(new OwnerWarning("rating", $"{names.GetValueOrDefault(r.HotelId, r.HotelId)}: คะแนนรีวิวจริงเฉลี่ย {r.Avg:0.0} จาก {r.Count} รีวิว",
                    $"/Stays/Details/{r.HotelId}#reviews"));
        }
        var staleBefore = DateTime.UtcNow - StaleAfter;
        var stale = await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending && (x.HotelSentAtUtc ?? x.CreatedAtUtc) < staleBefore);
        // Owner cannot open the support tools, so this warning has no link (Lodgo support follows it up).
        if (stale > 0) list.Add(new OwnerWarning("stale", $"คำขอจองรอโรงแรมยืนยันเกิน 24 ชม. {stale} รายการ · Lodgo support กำลังติดตาม", ""));
        return warnings = list;
    }

    public async Task<int> WarningCountAsync() => (await WarningsAsync()).Count;
}
