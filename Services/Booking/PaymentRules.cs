using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// Money rules of the demo wallet and of the accounts (AC) payment check.
/// Pure: amounts and statuses go in, a yes/no or a ledger description comes out. The services do the locking,
/// the reading and the writing; these rules decide what is allowed and what each ledger line says.
/// </summary>
public static class PaymentRules
{
    // ---------- Wallet top-up ----------
    // static readonly, not const: a "const decimal" is not a real CLR constant (the compiler copies the value into every
    // caller and also emits a static constructor that nothing ever runs), so the limits would live in many places.
    public static readonly decimal MinTopUp = 100m;
    public static readonly decimal MaxTopUp = 50000m;
    /// <summary>Highest balance a demo wallet may hold, so a runaway test cannot create silly numbers.</summary>
    public static readonly decimal MaxBalance = 1000000m;
    public const string TopUpAmountRule = "จำนวนเครดิตต้องเป็น 100–50,000 บาท และมีทศนิยมไม่เกิน 2 ตำแหน่ง";
    public const string MaxBalanceRule = "ยอดเครดิตจำลองสูงสุด 1,000,000 บาท";
    public const string TopUpDescription = "เติมเครดิตด้วยการโอนจำลอง";

    // ---------- Booking payment ----------
    public const string BadRequestIdRule = "รายการไม่ถูกต้อง กรุณาเริ่มจองใหม่";
    public const string TransferEvidenceRule = "แนบสลิปหรือกรอกเลขที่รายการโอนอย่างน้อยหนึ่งอย่าง";
    public const string NotEnoughCreditRule = "เครดิตไม่เพียงพอ กรุณาเติมเครดิตก่อนยืนยันการจอง";
    /// <summary>Unchecked transfers hold rooms, so one customer may keep only a few waiting at once.</summary>
    public const int MaxOpenTransfers = 3;
    public static string TooManyOpenTransfersRule =>
        $"มีการจองแบบโอนเงินรอตรวจอยู่ {MaxOpenTransfers} รายการแล้ว รอให้ฝ่ายบัญชี Lodgo ตรวจก่อน หรือยกเลิกรายการที่ไม่ใช้";
    public const int MaxPaymentRefLength = 64;
    public const int MaxDescriptionLength = 255;

    // ---------- Accounts (AC) check ----------
    public const int MaxRejectReasonLength = 120;
    public const string RejectReasonRule = "ระบุเหตุผลที่ไม่ผ่าน 1–120 ตัวอักษร";
    public static string NotWaitingForCheckRule(string status) =>
        $"รายการนี้ไม่ได้รอตรวจการชำระเงินแล้ว (สถานะ: {BookingStatus.Label(status)})";
    public const string RejectedReasonPrefix = "การชำระเงินไม่ผ่าน: ";
    public const string AccountsCheckedTransferNote = "ฝ่ายบัญชี Lodgo ตรวจการโอนเงินแล้ว · ส่งต่อให้โรงแรมยืนยันห้อง";
    public const string AccountsCheckedCreditNote = "ฝ่ายบัญชี Lodgo ตรวจการชำระด้วยเครดิตแล้ว · ส่งต่อให้โรงแรมยืนยันห้อง";
    public const string WaitingTransferNote = "ส่งคำขอจองแบบโอนเงิน รอฝ่ายบัญชี Lodgo ตรวจการชำระเงิน";
    public const string WaitingCreditNote = "ส่งคำขอจองและชำระด้วยเครดิต รอฝ่ายบัญชี Lodgo ตรวจการชำระเงิน";

    /// <summary>Did the customer choose bank transfer (rather than paying from the wallet)?</summary>
    public static bool IsTransfer(string? paymentMethod) => paymentMethod == PaymentMethods.Transfer;

    /// <summary>The method stored on the booking: anything that is not a transfer is paid by credit.</summary>
    public static string MethodOf(string? paymentMethod) =>
        IsTransfer(paymentMethod) ? PaymentMethods.Transfer : PaymentMethods.Credit;

    /// <summary>Every booking and top-up carries a GUID the browser generated, so a resend cannot pay twice.</summary>
    public static bool RequestIdValid(string? requestId) => Guid.TryParse(requestId, out _);

    /// <summary>Top-up amount: 100–50,000 baht with at most two decimals, and a valid request id.</summary>
    public static bool TopUpValid(TopUpForm form) =>
        RequestIdValid(form.RequestId) && form.Amount >= MinTopUp && form.Amount <= MaxTopUp
        && decimal.Round(form.Amount, 2) == form.Amount;

    /// <summary>A top-up must not push the demo balance over <see cref="MaxBalance"/>.</summary>
    public static bool TopUpFitsBalance(decimal balance, decimal amount) => balance + amount <= MaxBalance;

    /// <summary>A transfer needs proof: the slip, the reference number, or both.</summary>
    public static bool TransferEvidenceGiven(string? paymentRef, string? slipFile) =>
        !string.IsNullOrWhiteSpace(paymentRef) || !string.IsNullOrEmpty(slipFile);

    /// <summary>A customer may keep at most <see cref="MaxOpenTransfers"/> unchecked transfers waiting.</summary>
    public static bool MayOpenAnotherTransfer(int openTransfers) => openTransfers < MaxOpenTransfers;

    /// <summary>Paying from the wallet needs the money to be there now (checked under the wallet row lock).</summary>
    public static bool CreditCovers(decimal balance, decimal total) => balance >= total;

    /// <summary>The reference number as stored: trimmed, cut to the column limit, and null when nothing was typed.</summary>
    public static string? CleanPaymentRef(string? paymentRef)
    {
        var reference = BookingText.Cut(paymentRef?.Trim() ?? "", MaxPaymentRefLength);
        return reference.Length > 0 ? reference : null;
    }

    /// <summary>Lodgo accounts must say why a payment did not pass: 1–120 characters.</summary>
    public static bool RejectReasonValid(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length <= MaxRejectReasonLength;

    /// <summary>The cancellation reason recorded when accounts turn a payment down.</summary>
    public static string RejectedNote(string reason) => RejectedReasonPrefix + reason.Trim();

    /// <summary>
    /// A booking is waiting for the accounts check, or the click is a repeat of an approval that already went through
    /// (status Pending and the hotel has been told), or it is in some other status and must not look like success.
    /// </summary>
    public static bool AwaitingCheck(StayOrder order) => order.Status == BookingStatus.PaymentReview;
    public static bool AlreadyChecked(StayOrder order) =>
        order.Status == BookingStatus.Pending && order.HotelSentAtUtc is not null;

    /// <summary>The first line of a booking's history: who is waiting for what.</summary>
    public static string WaitingNote(bool transfer) => transfer ? WaitingTransferNote : WaitingCreditNote;
    /// <summary>The history line when accounts pass the payment on to the hotel.</summary>
    public static string CheckedNote(bool transfer) => transfer ? AccountsCheckedTransferNote : AccountsCheckedCreditNote;

    /// <summary>The wallet line for credit taken at booking time, before accounts have checked it.</summary>
    public static string CreditHeldDescription(string hotelName) => "ชำระเครดิตรอตรวจ " + hotelName;

    /// <summary>
    /// An approved transfer is written as money in and then the booking paid from it, so the balance ends where it was,
    /// the ledger is complete, and a later cancellation refunds it as credit like any other booking.
    /// </summary>
    public static string TransferInDescription(int orderId, string? paymentRef) => BookingText.Cut(
        $"โอนเงินค่าจอง {BookingText.Number(orderId)} · ตรวจแล้ว" + (paymentRef is null ? "" : " · " + paymentRef),
        MaxDescriptionLength);

    public static string TransferPaidDescription(string hotelName) =>
        BookingText.Cut("ชำระด้วยเงินโอนรอยืนยัน " + hotelName, MaxDescriptionLength);
}
