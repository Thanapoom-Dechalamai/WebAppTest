namespace Nontiwat_7362_L002.ViewModels.Roles;
public record OwnerDashboard(int Customers, int Employees, int Hotels, int Provinces, int Bookings, int Cancelled, decimal BookingRevenue, decimal Refunds)
{
    // Last 12 Thai months, oldest first (design 4.7).
    public IReadOnlyList<MonthTotals> Months { get; init; } = [];
    // Months the charts plot something in (net revenue or bookings); top-up-only months do not count.
    public int ActiveMonths => Months.Count(x => x.NetRevenue != 0 || x.Bookings > 0);
    public IReadOnlyList<RankRow> TopHotels { get; init; } = [];
    public IReadOnlyList<RankRow> TopProvinces { get; init; } = [];
    public IReadOnlyList<OwnerWarning> Warnings { get; init; } = [];
}
