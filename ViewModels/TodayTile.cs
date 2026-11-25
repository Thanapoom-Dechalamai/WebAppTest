using System.Globalization;
namespace Nontiwat_7362_L002.ViewModels;

// One box in the "งานของคุณวันนี้" panel at the top of every role's home page.
// A zero is shown as "ไม่มีงานค้าง" (never hidden) so the user can see the job is done.
public record PaymentReviewRow(Nontiwat_7362_L002.Models.StayOrder Order, string CustomerName, string CustomerEmail);
public record TodayTile(string Label, string Value, string Hint, string Url, bool NeedsAction)
{
    public const string NothingToDo = "ไม่มีงานค้าง";

    public static TodayTile Count(string label, int count, string url, string hint, bool needsAction = true) =>
        new(label, count.ToString("N0", CultureInfo.InvariantCulture), needsAction && count == 0 ? NothingToDo : hint, url, needsAction && count > 0);

    public static TodayTile Money(string label, decimal amount, string url, string hint, bool needsAction = true) =>
        new(label, "฿" + amount.ToString("N0", CultureInfo.InvariantCulture), amount == 0 ? NothingToDo : hint, url, needsAction && amount > 0);

    // A figure to read, not work to do (a rate, a total).
    public static TodayTile Info(string label, string value, string hint, string url) => new(label, value, hint, url, false);
}
