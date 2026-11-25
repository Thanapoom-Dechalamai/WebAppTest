using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class HotelDeskService(
    IBookingUnitOfWork uow, IBookingStore store, IWalletStore walletStore, IHotelLedger hotelLedger,
    IActorDirectory actors, BookingRefunds refunds, BookingDecisions decisions, IThaiClock clock)
{
    private DateTime Today => clock.Today;

    public async Task DecideAsync(int actorId, int orderId, bool confirm, string? reason = null, bool simulated = false)
    {
        if (!confirm && !BookingTransitions.DeclineReasonValid(reason)) throw new ArgumentException(BookingTransitions.DeclineReasonRule);
        var snapshot = await store.PeekOrderAsync(orderId) ?? throw new ArgumentException("ไม่พบรายการจอง");
        await uow.BeginAsync();
        try
        {
            // Every confirmation/refund shares the customer wallet lock with customer and support cancellation.
            var wallet = await walletStore.LockWalletAsync(snapshot.UserId);
            var team = await store.LockHotelTeamAsync(snapshot.HotelId);
            var actorRole = simulated ? null : await actors.EnsureHotelTeamMemberAsync(actorId, team);
            var order = await store.LoadOrderAsync(orderId);
            var move = confirm ? BookingTransitions.Confirm(order, Today) : BookingTransitions.Decline(order, Today);
            if (move.Error is string error) throw new ArgumentException(error);
            if (move.AlreadyDone) { await uow.CommitAsync(); return; }
            if (confirm)
            {
                if (!simulated && await actors.HotelRecorderAsync(actorId, actorRole, team) is null)
                    throw new ArgumentException("โรงแรมนี้ยังไม่มีทีมพนักงาน ให้ Admin เพิ่มพนักงานก่อนยืนยันรายการ");
                order.Status = BookingStatus.Confirmed;
                await decisions.RecordAsync(order, actorId, BookingStatus.Confirmed, move.Note + (simulated ? BookingText.DemoNote : ""));
            }
            else
            {
                // The hotel turned the request down: refund the credit and give the coupon back.
                await refunds.RefundAsync(order, wallet, actorId, BookingTransitions.DeclineNote(reason!), CancelledBy.Hotel);
            }
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }

    // Stays whose dates have passed with nobody acting: no-show or completed (BookingLifecycle). Run by BookingHousekeeping.
    // The system acts, so the hotel-team check is skipped; the hotel's share is recorded the same way as a hotel's own no-show.
    public async Task<int> CloseOverdueAsync()
    {
        var closed = 0;
        foreach (var (orderId, userId) in await store.OverdueStaysAsync(Today, 100))
        {
            uow.ClearChangeTracker();
            await uow.BeginAsync();
            try
            {
                await walletStore.LockWalletAsync(userId);
                var order = await store.LoadOrderAsync(orderId);
                var team = await store.LockHotelTeamAsync(order.HotelId);
                if (BookingLifecycle.OverdueStep(order.Status, order.CheckOut, Today) is string step)
                {
                    order.Status = step;
                    if (BookingTransitions.EarnsReceipt(step) && !await hotelLedger.HasReceiptAsync(order.Id)
                        && await actors.HotelRecorderAsync(BookingText.SystemActorId, null, team) is int recorder)
                        await hotelLedger.AddReceiptAsync(order, recorder, order.Total);
                    await decisions.RecordAsync(order, BookingText.SystemActorId, step, BookingLifecycle.NoteFor(step));
                    if (step == BookingStatus.NoShow)
                        await decisions.NotifyHotelNoShowAsync(order);
                    await uow.SaveAsync();
                    closed++;
                }
                await uow.CommitAsync();
            }
            catch { await uow.RollbackAsync(); throw; }
        }
        return closed;
    }

    // Confirmed -> CheckedIn -> Completed, or Confirmed -> NoShow when the guest never arrives.
    public async Task AdvanceAsync(int actorId, int orderId, string next, bool simulated = false)
    {
        if (!BookingTransitions.IsStayStep(next)) throw new ArgumentException(BookingTransitions.UnknownStepRule);
        var snapshot = await store.PeekOrderAsync(orderId) ?? throw new ArgumentException("ไม่พบการจอง");
        await uow.BeginAsync();
        try
        {
            await walletStore.LockWalletAsync(snapshot.UserId);
            var team = await store.LockHotelTeamAsync(snapshot.HotelId);
            var actorRole = simulated ? null : await actors.EnsureHotelTeamMemberAsync(actorId, team);
            var order = await store.LoadOrderAsync(orderId);
            var move = BookingTransitions.Advance(order, next, Today, simulated);
            if (move.AlreadyDone) { await uow.CommitAsync(); return; }
            if (move.Error is string error) throw new ArgumentException(error);
            order.Status = next;
            // The hotel earns its share once the guest arrived or did not come (FinanceRules.EarnedStatuses).
            if (BookingTransitions.EarnsReceipt(next) && !await hotelLedger.HasReceiptAsync(order.Id)
                && await actors.HotelRecorderAsync(actorId, actorRole, team) is int recorder)
                await hotelLedger.AddReceiptAsync(order, recorder, order.Total);
            await decisions.RecordAsync(order, actorId, next, move.Note + (simulated ? BookingText.DemoNote : ""));
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }
}
