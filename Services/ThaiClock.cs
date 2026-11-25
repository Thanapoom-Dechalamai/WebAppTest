namespace Nontiwat_7362_L002.Services;

/// <summary>
/// Lodgo's single source of "now", in Thai time (UTC+7, no daylight saving).
/// Every rule that depends on the date (can this booking still be cancelled, which month is open, what is "today"
/// on a dashboard) asks this instead of reading the system clock, so tests can pin the date.
/// </summary>
public interface IThaiClock
{
    /// <summary>The current instant in UTC (what the database stores).</summary>
    DateTime UtcNow { get; }

    /// <summary>Today's date in Thailand, at midnight, with no time zone kind (the form the app compares dates in).</summary>
    DateTime Today { get; }

    /// <summary>The first day of the current Thai month (how <see cref="ThaiMonth"/> represents a month).</summary>
    DateTime ThisMonth { get; }
}

/// <summary>
/// <see cref="IThaiClock"/> on top of .NET's <see cref="TimeProvider"/>: the site uses <see cref="TimeProvider.System"/>,
/// tests use <c>FakeTimeProvider</c> to freeze or move time.
/// </summary>
public sealed class ThaiClock(TimeProvider time) : IThaiClock
{
    /// <summary>Thailand is UTC+7 all year.</summary>
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    public DateTime UtcNow => time.GetUtcNow().UtcDateTime;

    public DateTime Today => DateTime.SpecifyKind(time.GetUtcNow().ToOffset(Offset).Date, DateTimeKind.Unspecified);

    public DateTime ThisMonth => ThaiMonth.Of(Today);
}
