using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class PaymentReviewService(
    IBookingUnitOfWork uow, IBookingStore store, IWalletStore walletStore, IBookingJournal journal,
    IActorDirectory actors, BookingRefunds refunds, IThaiClock clock)
{
    public async Task ApprovePaymentAsync(int actorId, int orderId, bool simulated = false)
    {
        var snapshot = await store.PeekOrderAsync(orderId) ?? throw new ArgumentException("ไม่พบรายการจอง");
        await walletStore.EnsureWalletAsync(snapshot.UserId);
        await uow.BeginAsync();
        try
        {
            var wallet = await walletStore.LockWalletAsync(snapshot.UserId);
            var order = await store.LoadOrderAsync(orderId);
            if (!PaymentRules.AwaitingCheck(order))
            {
                // A second click on the same approval is fine; anything else must not look like success.
                if (PaymentRules.AlreadyChecked(order)) { await uow.CommitAsync(); return; }
                throw new ArgumentException(PaymentRules.NotWaitingForCheckRule(order.Status));
            }
            // The transfer is money in, then the booking is paid from it: credit stays the same, the ledger is complete,
            // and a later cancellation refunds it as credit like any other booking.
            var transfer = PaymentRules.IsTransfer(order.PaymentMethod);
            if (transfer)
            {
                await walletStore.AddEntryAsync(wallet, "transfer:" + order.Id, "TopUp", order.Total,
                    PaymentRules.TransferInDescription(order.Id, order.PaymentRef));
                await walletStore.AddEntryAsync(wallet, "order:" + order.RequestId, "Payment", -order.Total,
                    PaymentRules.TransferPaidDescription(order.HotelName));
            }
            order.Status = BookingStatus.Pending;
            order.HotelSentAtUtc = clock.UtcNow;
            var actor = await actors.DescribeAsync(actorId);
            await journal.AddEventAsync(order, BookingStatus.Pending, PaymentRules.CheckedNote(transfer) + (simulated ? BookingText.DemoNote : ""),
                actorId == BookingText.SystemActorId ? null : actorId, actor.Label);
            await journal.NotifyCustomerAsync(order, "ตรวจการชำระเงินแล้ว", $"{BookingText.Number(order.Id)} · ส่งต่อให้ {order.HotelName} ยืนยันห้อง");
            await journal.NotifyHotelTeamAsync(order, "คำขอจองใหม่ · รอยืนยัน",
                $"{BookingText.Number(order.Id)} · {order.RoomName} {order.Rooms} ห้อง · {ThaiDates.Short(order.CheckIn)}–{ThaiDates.Short(order.CheckOut)}");
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }

    public async Task RejectPaymentAsync(int actorId, int orderId, string? reason)
    {
        if (!PaymentRules.RejectReasonValid(reason)) throw new ArgumentException(PaymentRules.RejectReasonRule);
        var snapshot = await store.PeekOrderAsync(orderId) ?? throw new ArgumentException("ไม่พบรายการจอง");
        await walletStore.EnsureWalletAsync(snapshot.UserId);
        await uow.BeginAsync();
        try
        {
            var wallet = await walletStore.LockWalletAsync(snapshot.UserId);
            var order = await store.LoadOrderAsync(orderId);
            if (!PaymentRules.AwaitingCheck(order)) throw new ArgumentException(PaymentRules.NotWaitingForCheckRule(order.Status));
            await refunds.RefundAsync(order, wallet, actorId, PaymentRules.RejectedNote(reason!), CancelledBy.Accounts);
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }
}
