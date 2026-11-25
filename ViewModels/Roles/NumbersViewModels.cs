namespace Nontiwat_7362_L002.ViewModels.Roles;

// Money and bookings of one Thai calendar month, counted the way design 4.5 says:
// money that moved (payments, refunds, top-ups, payouts) by the entry's date (Thai time);
// hotel earnings and commission by the check-out date of finished stays (Completed/NoShow);
// booking counts by the booking's creation date (Thai time).
public record MonthTotals(DateTime Month, decimal Revenue, decimal Refunds, decimal TopUps, decimal Commission,
    decimal HotelEarnings, decimal Payouts, int Bookings, int Cancelled)
{
    public decimal NetRevenue => Revenue - Refunds;
    public bool HasActivity => Revenue != 0 || Refunds != 0 || TopUps != 0 || Commission != 0 || HotelEarnings != 0 || Payouts != 0 || Bookings != 0;
}
