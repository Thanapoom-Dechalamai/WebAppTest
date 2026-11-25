using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services;

public record SlowHotel(string HotelId, string Name, double AvgHours, int Confirmed);

// Rates are null when there is nothing to divide by; the page shows "—" instead of 0% or NaN.
public record KpiPage(int Days, int Bookings, int Cancelled, int NoShow, double? CancelRate, double? NoShowRate,
    double? AvgConfirmHours, int ConfirmedCount, int StaleRequests, int PromoUses, decimal PromoDiscount, decimal PromoSales,
    IReadOnlyList<SlowHotel> Slowest, double? RealReviewAverage, int RealReviewCount);

// Manager KPIs for the last N days. Everything is about bookings created in the window (design 4.5).
public class KpiService(ApplicationDbContext db)
{
    public static readonly int[] AllowedDays = [7, 30, 90];
    public static int NormalizeDays(int? days) => days is int d && AllowedDays.Contains(d) ? d : 30;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);

    public async Task<KpiPage> ForAsync(int days)
    {
        days = NormalizeDays(days);
        var since = DateTime.UtcNow.AddDays(-days);
        // Only bookings that reached a hotel: a transfer Lodgo rejected is not the hotel's cancellation.
        var orders = db.Orders.AsNoTracking().Where(x => x.CreatedAtUtc >= since && (x.PaymentMethod == PaymentMethods.Credit || x.HotelSentAtUtc != null));
        var bookings = await orders.CountAsync();
        var cancelled = await orders.CountAsync(x => x.Status == BookingStatus.Cancelled);
        var noShow = await orders.CountAsync(x => x.Status == BookingStatus.NoShow);
        double? Rate(int part) => bookings == 0 ? null : part * 100.0 / bookings;

        var confirms = await ConfirmTimesAsync(orders);
        var slowest = confirms.GroupBy(x => new { x.HotelId, x.HotelName }).Where(g => g.Count() >= 2)
            .Select(g => new SlowHotel(g.Key.HotelId, g.Key.HotelName, g.Average(x => x.Hours), g.Count()))
            .OrderByDescending(x => x.AvgHours).Take(5).ToList();
        var staleBefore = DateTime.UtcNow - StaleAfter;
        var stale = await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending && (x.HotelSentAtUtc ?? x.CreatedAtUtc) < staleBefore);

        var promos = await (from r in db.PromotionRedemptions.AsNoTracking() join o in orders on r.OrderId equals o.Id
                            select new { r.DiscountAmount, o.Total }).ToListAsync();
        var reviews = db.HotelReviews.AsNoTracking().Where(x => !x.IsSample && x.CreatedAtUtc >= since);
        var reviewCount = await reviews.CountAsync();
        double? reviewAverage = reviewCount == 0 ? null : await reviews.AverageAsync(x => (double)x.Rating);

        return new KpiPage(days, bookings, cancelled, noShow, Rate(cancelled), Rate(noShow),
            confirms.Count == 0 ? null : confirms.Average(x => x.Hours), confirms.Count, stale,
            promos.Count, promos.Sum(x => x.DiscountAmount), promos.Sum(x => x.Total), slowest, reviewAverage, reviewCount);
    }

    // Just the average confirmation time (the Manager "today" tile), without the rest of the page.
    public async Task<double?> AvgConfirmHoursAsync(int days)
    {
        var since = DateTime.UtcNow.AddDays(-NormalizeDays(days));
        var confirms = await ConfirmTimesAsync(db.Orders.AsNoTracking().Where(x => x.CreatedAtUtc >= since));
        return confirms.Count == 0 ? null : confirms.Average(x => x.Hours);
    }

    private record ConfirmTime(string HotelId, string HotelName, double Hours);

    // Time from booking to the hotel's first "Confirmed" (booking history first, the latest decision as fallback).
    private async Task<List<ConfirmTime>> ConfirmTimesAsync(IQueryable<StayOrder> orders)
    {
        var fromEvents = await (from e in db.BookingEvents.AsNoTracking() join o in orders on e.OrderId equals o.Id
                                where e.Status == BookingStatus.Confirmed
                                group e by new { o.Id, o.HotelId, o.HotelName, CreatedAtUtc = o.HotelSentAtUtc ?? o.CreatedAtUtc } into g
                                select new { g.Key.Id, g.Key.HotelId, g.Key.HotelName, g.Key.CreatedAtUtc, At = g.Min(x => x.CreatedAtUtc) }).ToListAsync();
        var withEvent = fromEvents.Select(x => x.Id).ToHashSet();
        var fromDecisions = await (from d in db.HotelDecisions.AsNoTracking() join o in orders on d.OrderId equals o.Id
                                   where d.Action == BookingStatus.Confirmed
                                   select new { o.Id, o.HotelId, o.HotelName, CreatedAtUtc = o.HotelSentAtUtc ?? o.CreatedAtUtc, At = d.UpdatedAtUtc }).ToListAsync();
        return fromEvents.Concat(fromDecisions.Where(x => !withEvent.Contains(x.Id)))
            .Select(x => new ConfirmTime(x.HotelId, x.HotelName, Math.Max(0, (x.At - x.CreatedAtUtc).TotalHours))).ToList();
    }

    public static string Hours(double? hours) => hours switch
    {
        null => "—",
        < 1 => $"{hours.Value * 60:0} นาที",
        < 48 => $"{hours.Value:0.#} ชม.",
        _ => $"{hours.Value / 24:0.#} วัน"
    };
}
