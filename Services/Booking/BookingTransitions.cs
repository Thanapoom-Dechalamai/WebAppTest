using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// The answer to "may this booking move to that status now?": go ahead (with the note for the booking history),
/// nothing to do because it is already there (a second click), or no with the reason shown to the user.
/// </summary>
public readonly record struct Transition(bool Apply, bool AlreadyDone, string? Error, string? Note)
{
    public static Transition Go(string? note = null) => new(true, false, null, note);
    public static Transition Done => new(false, true, null, null);
    public static Transition Refuse(string error) => new(false, false, error, null);
}

/// <summary>
/// Status rules of a booking (pure: the order and "today" go in, a <see cref="Transition"/> comes out).
/// <code>
/// PaymentReview ──AC──► Pending ──hotel──► Confirmed ──► CheckedIn ──► Completed
///       │                  │                   │  └────► NoShow
///       └──────────────────┴───────────────────┴──► Cancelled  (refund, see RefundPolicy)
/// </code>
/// The services apply these rules after locking the rows, so two clicks at the same time see the same answer.
/// </summary>
public static class BookingTransitions
{
    public const int MaxReasonLength = 120;
    public const string DeclineReasonRule = "ระบุเหตุผลยกเลิก 1–120 ตัวอักษร";
    public const string AwaitingAccountsRule = "รายการนี้ยังรอฝ่ายบัญชี Lodgo ตรวจการชำระเงิน";
    public const string AlreadyCancelledRule = "รายการนี้ยกเลิกไปแล้ว";
    public const string ConfirmTooLateRule = "ไม่สามารถยืนยันรายการที่เลยวันเช็กอินแล้ว";
    public const string DeclineHint = " หากลูกค้าไม่มาเข้าพัก ให้กด \"ลูกค้าไม่มา\"";
    public const string UnknownStepRule = "ขั้นตอนไม่ถูกต้อง";
    public const string CheckInNeedsConfirmedRule = "เช็กอินได้เฉพาะรายการที่ยืนยันแล้ว";
    public const string CheckInDatesRule = "เช็กอินได้ในช่วงวันที่เข้าพักเท่านั้น";
    public const string CheckOutNeedsCheckInRule = "เช็กเอาต์ได้เฉพาะรายการที่เช็กอินแล้ว";
    public const string NoShowNeedsConfirmedRule = "บันทึกลูกค้าไม่มาได้เฉพาะรายการที่ยืนยันแล้ว";
    public const string NoShowTooEarlyRule = "บันทึกลูกค้าไม่มาได้ตั้งแต่วันเช็กอินเป็นต้นไป";
    public const string SupportTooLateRule = "รายการนี้ยกเลิกไม่ได้ตามสถานะหรือวันเข้าพัก";

    public const string ConfirmedNote = "ห้องว่าง · พนักงานโรงแรมยืนยันการจอง";
    public const string CheckedInNote = "ลูกค้าเช็กอินแล้ว";
    public const string CompletedNote = "เช็กเอาต์และสิ้นสุดการเข้าพัก";
    public const string NoShowNote = "ลูกค้าไม่มาเข้าพัก · ปิดรายการโดยไม่คืนเครดิต";

    /// <summary>The hotel must say why it turns a request down: 1–120 characters (checked before anything is locked).</summary>
    public static bool DeclineReasonValid(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Length <= MaxReasonLength;

    /// <summary>The cancellation reason recorded when the hotel turns a request down.</summary>
    public static string DeclineNote(string reason) => "โรงแรมยกเลิก: " + reason.Trim();

    /// <summary>The hotel confirms the room: only a request waiting for the hotel, and not after its check-in day.</summary>
    public static Transition Confirm(StayOrder order, DateTime today)
    {
        if (order.Status == BookingStatus.PaymentReview) return Transition.Refuse(AwaitingAccountsRule);
        if (order.Status == BookingStatus.Cancelled) return Transition.Refuse(AlreadyCancelledRule);
        if (order.Status == BookingStatus.Confirmed) return Transition.Done;
        if (order.Status != BookingStatus.Pending || order.CheckIn < today) return Transition.Refuse(ConfirmTooLateRule);
        return Transition.Go(ConfirmedNote);
    }

    /// <summary>The hotel turns the request down (or cancels a confirmed stay before its check-in day).</summary>
    public static Transition Decline(StayOrder order, DateTime today)
    {
        if (order.Status == BookingStatus.PaymentReview) return Transition.Refuse(AwaitingAccountsRule);
        if (order.Status == BookingStatus.Cancelled) return Transition.Done;
        if (!BookingRules.CanCancel(order, today)) return Transition.Refuse(BookingRules.CancelRuleMessage + DeclineHint);
        return Transition.Go();
    }

    /// <summary>The three steps a hotel can take after confirming: check-in, check-out (Completed) and no-show.</summary>
    public static bool IsStayStep(string next) =>
        next is BookingStatus.CheckedIn or BookingStatus.Completed or BookingStatus.NoShow;

    /// <summary>
    /// Move a confirmed stay on. Demo mode (<paramref name="simulated"/>) may check in on any day so a customer can walk
    /// through the whole flow; it still cannot mark a no-show before the check-in day.
    /// </summary>
    public static Transition Advance(StayOrder order, string next, DateTime today, bool simulated)
    {
        if (!IsStayStep(next)) return Transition.Refuse(UnknownStepRule);
        if (order.Status == next) return Transition.Done;
        switch (next)
        {
            case BookingStatus.CheckedIn:
                if (order.Status != BookingStatus.Confirmed) return Transition.Refuse(CheckInNeedsConfirmedRule);
                if (!simulated && (today < order.CheckIn || today >= order.CheckOut)) return Transition.Refuse(CheckInDatesRule);
                return Transition.Go(CheckedInNote);
            case BookingStatus.Completed:
                if (order.Status != BookingStatus.CheckedIn) return Transition.Refuse(CheckOutNeedsCheckInRule);
                return Transition.Go(CompletedNote);
            default:
                if (order.Status != BookingStatus.Confirmed) return Transition.Refuse(NoShowNeedsConfirmedRule);
                if (today < order.CheckIn) return Transition.Refuse(NoShowTooEarlyRule);
                return Transition.Go(NoShowNote);
        }
    }

    /// <summary>The hotel earns its share once the guest arrived or did not come (see <see cref="FinanceRules.EarnedStatuses"/>).</summary>
    public static bool EarnsReceipt(string next) => next is BookingStatus.CheckedIn or BookingStatus.NoShow;

    /// <summary>
    /// The same date and status limits apply when Lodgo support cancels on the customer's behalf.
    /// </summary>
    public static Transition Cancel(StayOrder order, DateTime today, bool bySupport)
    {
        if (order.Status == BookingStatus.Cancelled) return Transition.Done;
        if (!BookingRules.CanCancel(order, today)) return Transition.Refuse(bySupport ? SupportTooLateRule : BookingRules.CancelRuleMessage);
        return Transition.Go();
    }
}
