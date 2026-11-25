using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels;

public record GuestRow(StayOrder Order, string GuestName, string GuestEmail);

// The front desk's day: who to check in, who is due out, and everyone staying now.
// Late arrivals (check-in date already passed, still Confirmed) stay on the arrivals list.
public record GuestBoard(IReadOnlyList<GuestRow> Arrivals, IReadOnlyList<GuestRow> DueOut, IReadOnlyList<GuestRow> InHouse)
{
    public static GuestBoard Split(IEnumerable<GuestRow> rows, DateTime today)
    {
        var list = rows.ToList();
        return new GuestBoard(
            list.Where(x => x.Order.Status == BookingStatus.Confirmed && x.Order.CheckIn.Date <= today.Date).OrderBy(x => x.Order.CheckIn).ThenBy(x => x.Order.Id).ToList(),
            list.Where(x => x.Order.Status == BookingStatus.CheckedIn && x.Order.CheckOut.Date <= today.Date).OrderBy(x => x.Order.CheckOut).ThenBy(x => x.Order.Id).ToList(),
            list.Where(x => x.Order.Status == BookingStatus.CheckedIn).OrderBy(x => x.Order.CheckOut).ThenBy(x => x.Order.Id).ToList());
    }
}
