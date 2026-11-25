namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>Text helpers shared by the booking rules: column limits and the booking number customers see.</summary>
public static class BookingText
{
    /// <summary>The actor id used when the system itself did something (the expiry sweep has no person behind it).</summary>
    public const int SystemActorId = 0;
    internal const string DemoNote = " · ทำแทนทีมงานในโหมดสาธิต";

    /// <summary>Cut text to a column limit (the database would reject it otherwise). Shorter text is left as it is.</summary>
    public static string Cut(string text, int max) => text.Length > max ? text[..max] : text;

    /// <summary>The booking number printed everywhere: LG and six digits, e.g. LG000042.</summary>
    public static string Number(int orderId) => $"LG{orderId:D6}";
}
