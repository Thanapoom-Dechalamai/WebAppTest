using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels.Roles;

namespace Nontiwat_7362_L002.Services;

// Thai calendar months. A month is represented by its first day (no time, no kind).
public static class ThaiMonth
{
    private static readonly CultureInfo Thai = new("th-TH") { DateTimeFormat = { Calendar = new GregorianCalendar() } };
    public static DateTime Of(DateTime date) => new(date.Year, date.Month, 1);
    // The database stores UTC; Thai time is UTC+7, so a Thai month starts 7 hours earlier in UTC.
    public static DateTime StartUtc(DateTime month) => month.AddHours(-7);
    public static DateTime EndUtc(DateTime month) => month.AddMonths(1).AddHours(-7);
    public static string Key(DateTime month) => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    public static string Label(DateTime month) => month.ToString("MMMM yyyy", Thai);
    public static string Short(DateTime month) => month.ToString("MMM yy", Thai);

    public static DateTime? Parse(string? yyyyMM) =>
        DateTime.TryParseExact(yyyyMM, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
        && month.Year is >= 2000 and <= 2100 ? month : null;
}

// Aggregates raw rows into the per-month figures shown on the Owner dashboard.
public class MonthlyFigures(ApplicationDbContext db)
{
    public const int MaxMonths = 24;

    // Every month from 'fromMonth' to 'toMonth' inclusive (empty months included), oldest first.
    public async Task<IReadOnlyList<MonthTotals>> RangeAsync(DateTime fromMonth, DateTime toMonth)
    {
        fromMonth = ThaiMonth.Of(fromMonth); toMonth = ThaiMonth.Of(toMonth);
        if (toMonth < fromMonth) (fromMonth, toMonth) = (toMonth, fromMonth);
        if (((toMonth.Year - fromMonth.Year) * 12 + toMonth.Month - fromMonth.Month) >= MaxMonths)
            fromMonth = toMonth.AddMonths(-(MaxMonths - 1));
        var startUtc = ThaiMonth.StartUtc(fromMonth); var endUtc = ThaiMonth.EndUtc(toMonth);
        var endDate = toMonth.AddMonths(1);

        var money = await db.WalletEntries.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .GroupBy(x => new { x.CreatedAtUtc.AddHours(7).Year, x.CreatedAtUtc.AddHours(7).Month, x.Kind })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Kind, Total = g.Sum(x => x.Amount) }).ToListAsync();
        var payouts = await db.HotelPayouts.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .GroupBy(x => new { x.CreatedAtUtc.AddHours(7).Year, x.CreatedAtUtc.AddHours(7).Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) }).ToListAsync();
        // Per-booking totals: commission is rounded per booking, exactly like the Settlement page.
        var earned = await db.Orders.AsNoTracking()
            .Where(x => FinanceRules.EarnedStatuses.Contains(x.Status) && x.CheckOut >= fromMonth && x.CheckOut < endDate)
            .Select(x => new { x.CheckOut, x.Total, x.CommissionPercent }).ToListAsync();
        var bookings = await db.Orders.AsNoTracking()
            .Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .GroupBy(x => new { x.CreatedAtUtc.AddHours(7).Year, x.CreatedAtUtc.AddHours(7).Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Cancelled = g.Count(x => x.Status == BookingStatus.Cancelled) }).ToListAsync();

        var result = new List<MonthTotals>();
        for (var m = fromMonth; m <= toMonth; m = m.AddMonths(1))
        {
            decimal Kind(string kind) => money.Where(x => x.Year == m.Year && x.Month == m.Month && x.Kind == kind).Sum(x => x.Total);
            var stays = earned.Where(x => x.CheckOut.Year == m.Year && x.CheckOut.Month == m.Month).ToList();
            var commission = FinanceRules.CommissionOf(stays.Select(x => (x.Total, x.CommissionPercent)));
            var b = bookings.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            result.Add(new MonthTotals(m, -Kind("Payment"), Kind("Refund"), Kind("TopUp"), commission, stays.Sum(x => x.Total) - commission,
                payouts.Where(x => x.Year == m.Year && x.Month == m.Month).Sum(x => x.Total), b?.Count ?? 0, b?.Cancelled ?? 0));
        }
        return result;
    }

    // First month that has any booking or money movement (Thai time); null on an empty database.
    public async Task<DateTime?> FirstActivityMonthAsync()
    {
        var firstEntry = await db.WalletEntries.AsNoTracking().MinAsync(x => (DateTime?)x.CreatedAtUtc);
        var firstOrder = await db.Orders.AsNoTracking().MinAsync(x => (DateTime?)x.CreatedAtUtc);
        var first = new[] { firstEntry, firstOrder }.Where(x => x is not null).Min();
        return first is DateTime utc ? ThaiMonth.Of(utc.AddHours(7)) : null;
    }
}
