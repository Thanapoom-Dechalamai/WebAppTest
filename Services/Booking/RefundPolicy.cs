using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>Who ends a booking early, which decides whether the promotion code is given back.</summary>
public enum CancelledBy
{
    /// <summary>The customer changed their mind: the code stays used (their choice).</summary>
    Customer,
    /// <summary>Lodgo support stepped in before the hotel answered: the customer did nothing wrong.</summary>
    Support,
    /// <summary>The hotel turned the request down.</summary>
    Hotel,
    /// <summary>Lodgo accounts did not accept the payment.</summary>
    Accounts,
    /// <summary>Nobody confirmed before the check-in day, so the system closed it.</summary>
    System,
}

/// <summary>What a cancellation does to money and the coupon. Every field is decided by <see cref="RefundPolicy.For"/>.</summary>
/// <param name="RefundCredit">Put the booking total back in the customer's wallet (only if the money was really taken).</param>
/// <param name="Amount">How much goes back: always the full total the customer paid.</param>
/// <param name="RestoreCoupon">Give the promotion code back so it can be used again.</param>
/// <param name="ReverseReceipt">Cancel the hotel's receipt, if it has one, with a refund line of the same amount.</param>
public readonly record struct RefundDecision(bool RefundCredit, decimal Amount, bool RestoreCoupon, bool ReverseReceipt);

/// <summary>
/// Refund rules in one place, used by every way a booking can be cancelled (customer, support, hotel, accounts, expiry).
/// Pure: the order goes in, a <see cref="RefundDecision"/> comes out; the service writes the ledger lines.
/// </summary>
public static class RefundPolicy
{
    public const string ExpiredReason = "หมดเวลา: การจองยังไม่ได้รับการยืนยันเมื่อถึงวันเช็กอิน";
    public const string CustomerReason = "ลูกค้ายกเลิกรายการ";

    /// <summary>
    /// The money was taken when: the customer paid by credit (taken at booking time), or the booking has already passed the
    /// accounts check. A bank transfer still waiting for that check was never counted as money in, so nothing goes back.
    /// </summary>
    public static bool WasPaid(StayOrder order) =>
        order.Status != BookingStatus.PaymentReview || order.PaymentMethod == PaymentMethods.Credit;

    /// <summary>Only a customer who cancels their own booking keeps the coupon used; everyone else gives it back.</summary>
    public static bool RestoresCoupon(CancelledBy by) => by != CancelledBy.Customer;

    /// <summary>Decide the refund for <paramref name="order"/> before its status is set to Cancelled.</summary>
    public static RefundDecision For(StayOrder order, CancelledBy by)
    {
        var paid = WasPaid(order);
        return new(paid, paid ? order.Total : 0m, RestoresCoupon(by), ReverseReceipt: true);
    }

    /// <summary>The wallet line for a refund, e.g. "คืนเครดิตรายการ LG000042 · ลูกค้ายกเลิกรายการ".</summary>
    public static string RefundDescription(int orderId, string reason) =>
        $"คืนเครดิตรายการ {BookingText.Number(orderId)} · {reason}";

    /// <summary>The cancellation note gets the returned code added, so the history says the coupon came back.</summary>
    public static string WithCouponNote(string reason, string code) => reason + " · คืนสิทธิ์โค้ด " + code;
}
