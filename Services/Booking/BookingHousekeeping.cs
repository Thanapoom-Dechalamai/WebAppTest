namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// Hourly job (owner's choice, 2026-11-25): closes bookings whose dates passed with nobody acting (BookingLifecycle):
/// unconfirmed on the check-in day → cancelled with a refund; confirmed but never checked in → no-show; checked in past
/// check-out → completed. Runs once shortly after start, then every hour. Turn off with "Housekeeping": { "Enabled": false }.
/// A failed run is logged and the next run tries again; it never stops the site.
/// </summary>
public sealed class BookingHousekeeping(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<BookingHousekeeping> log) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Housekeeping:Enabled", true)) return;
        try { await Task.Delay(FirstRunDelay, stoppingToken); } catch (OperationCanceledException) { return; }
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync();
        } while (await WaitAsync(timer, stoppingToken));
    }

    public async Task RunOnceAsync()
    {
        try
        {
            // Each call handles up to one batch; keep going until a batch comes back short, so a backlog (e.g. the first
            // run on an old database) is cleared in one run. MaxBatches stops a runaway loop.
            var expired = await DrainAsync(sp => sp.GetRequiredService<CancellationService>().ExpireStalePendingAsync());
            var closed = await DrainAsync(sp => sp.GetRequiredService<HotelDeskService>().CloseOverdueAsync());
            if (expired + closed > 0) log.LogInformation("Booking housekeeping: {Expired} expired, {Closed} closed as no-show or completed", expired, closed);
        }
        catch (Exception error)
        {
            log.LogError(error, "Booking housekeeping failed; it will try again in {Interval}", Interval);
        }
    }

    public const int BatchSize = 100; // CancellationService and HotelDeskService read 100 bookings per call
    private const int MaxBatches = 50;

    // A fresh scope (and DbContext) per batch keeps memory flat while a backlog is cleared.
    private async Task<int> DrainAsync(Func<IServiceProvider, Task<int>> batch)
    {
        var total = 0;
        for (var i = 0; i < MaxBatches; i++)
        {
            await using var scope = scopes.CreateAsyncScope();
            var done = await batch(scope.ServiceProvider);
            total += done;
            if (done < BatchSize) break;
        }
        return total;
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }
}
