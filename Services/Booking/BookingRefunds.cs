using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>Applies the shared refund policy within the calling workflow's transaction.</summary>
public sealed class BookingRefunds(IBookingStore store, IWalletStore walletStore, IHotelLedger hotelLedger, BookingDecisions decisions)
{
    internal async Task RefundAsync(StayOrder order, MemberWallet wallet, int actorId, string reason, CancelledBy by)
    {
        var refund = RefundPolicy.For(order, by);
        order.Status = BookingStatus.Cancelled;
        if (refund.RefundCredit)
            await walletStore.AddEntryAsync(wallet, "refund:" + order.Id, "Refund", refund.Amount,
                RefundPolicy.RefundDescription(order.Id, reason));
        // Reverse the hotel's receipt, recorded against the same recorder as the original receipt.
        var receipt = refund.ReverseReceipt ? await hotelLedger.FindReceiptAsync(order.Id) : null;
        if (receipt is not null) await hotelLedger.AddReceiptReversalAsync(receipt);
        if (refund.RestoreCoupon)
        {
            var redemption = await store.FindRedemptionAsync(order.Id);
            if (redemption is not null)
            {
                await store.RemoveRedemptionAsync(redemption);
                reason = RefundPolicy.WithCouponNote(reason, redemption.Code);
            }
        }
        await decisions.RecordAsync(order, actorId, BookingStatus.Cancelled, reason, refunded: refund.RefundCredit);
    }
}
