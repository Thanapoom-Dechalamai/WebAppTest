using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// What happens to a booking when its dates pass and nobody acted (owner's rules, 2026-11-25). Pure: dates are Thai dates.
/// <list type="bullet">
/// <item>Not yet through accounts or the hotel when the check-in day arrives: cancelled with a refund (CancellationService).</item>
/// <item>Confirmed but never checked in by the check-out day: closed as a no-show, no refund (the hotel keeps its share).</item>
/// <item>Checked in and the check-out day has passed: closed as completed, so the guest can review.</item>
/// </list>
/// Run every hour by <c>BookingHousekeeping</c> and also when booking pages open.
/// </summary>
public static class BookingLifecycle
{
    public const string AutoNoShowNote = "ระบบปิดอัตโนมัติ: เลยวันเช็กเอาต์แล้วโดยไม่ได้เช็กอิน · ไม่คืนเครดิต";
    public const string AutoCompletedNote = "ระบบปิดอัตโนมัติ: เลยวันเช็กเอาต์แล้ว · เข้าพักเสร็จสิ้น";

    public static bool ShouldExpire(string status, DateTime checkIn, DateTime today) =>
        status is BookingStatus.Pending or BookingStatus.PaymentReview && checkIn.Date <= today.Date;

    // The automatic next status for a stay whose dates have passed, or null when nothing is due.
    public static string? OverdueStep(string status, DateTime checkOut, DateTime today) => status switch
    {
        BookingStatus.Confirmed when checkOut.Date <= today.Date => BookingStatus.NoShow,
        BookingStatus.CheckedIn when checkOut.Date < today.Date => BookingStatus.Completed,
        _ => null
    };

    public static string NoteFor(string step) => step == BookingStatus.NoShow ? AutoNoShowNote : AutoCompletedNote;
}
