using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class CancellationService(
    IBookingUnitOfWork uow, IBookingStore store, IWalletStore walletStore, BookingRefunds refunds, IThaiClock clock)
{
    private DateTime Today => clock.Today;

    // Customer cancels their own booking. The coupon stays used (customer's choice).
    public Task CancelAsync(int userId, int orderId) =>
        CancelForUserAsync(userId, orderId, userId, RefundPolicy.CustomerReason, CancelledBy.Customer);

    // Lodgo support follows the booking's cancellation deadline and restores eligible codes.
    public Task SupportCancelAsync(int actorId, int orderId, int customerId, string reason) =>
        CancelForUserAsync(customerId, orderId, actorId, reason, CancelledBy.Support);

    public Task CancelForCaseAsync(int actorId, int caseId, int orderId, int customerId, string reason) =>
        CancelForUserAsync(customerId, orderId, actorId, reason, CancelledBy.Support, caseId);

    private async Task CancelForUserAsync(int userId, int orderId, int actorId, string note, CancelledBy by, int? caseId = null)
    {
        await walletStore.EnsureWalletAsync(userId);
        await uow.BeginAsync();
        try
        {
            var wallet = await walletStore.LockWalletAsync(userId);
            if (caseId is int supportId)
            {
                var item = await store.LockSupportCaseAsync(supportId);
                if (item is null || item.OrderId != orderId || item.CustomerId != userId || item.Status == SupportStatus.Closed)
                    throw new ArgumentException("ไม่พบการจองที่แก้ไขได้ในเรื่องนี้");
            }
            var order = await store.LoadOrderForUserAsync(orderId, userId) ?? throw new ArgumentException("ไม่พบรายการจอง");
            var move = BookingTransitions.Cancel(order, Today, bySupport: by == CancelledBy.Support);
            if (move.AlreadyDone) { await uow.CommitAsync(); return; }
            if (move.Error is string error) throw new ArgumentException(error);
            await refunds.RefundAsync(order, wallet, actorId, note, by);
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }

    // A request nobody confirmed by its check-in day cannot happen any more (BookingLifecycle.ShouldExpire).
    // Close it and give the customer's credit and coupon back instead of leaving it stuck.
    public async Task<int> ExpireStalePendingAsync()
    {
        var stale = await store.StaleUnconfirmedAsync(Today, 100);
        var closed = 0;
        foreach (var (orderId, userId) in stale)
        {
            // Read fresh rows for every order so a balance changed by another request is never overwritten.
            uow.ClearChangeTracker();
            await walletStore.EnsureWalletAsync(userId);
            await uow.BeginAsync();
            try
            {
                var wallet = await walletStore.LockWalletAsync(userId);
                var order = await store.LoadOrderAsync(orderId);
                if (BookingLifecycle.ShouldExpire(order.Status, order.CheckIn, Today))
                {
                    await refunds.RefundAsync(order, wallet, BookingText.SystemActorId, RefundPolicy.ExpiredReason, CancelledBy.System);
                    await uow.SaveAsync();
                    closed++;
                }
                await uow.CommitAsync();
            }
            catch { await uow.RollbackAsync(); throw; }
        }
        return closed;
    }
}
