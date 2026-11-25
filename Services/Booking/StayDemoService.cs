using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class StayDemoService(IBookingStore store, PaymentReviewService payments, HotelDeskService hotelDesk)
{
    public static string? NextStepLabel(string status) => status switch
    {
        BookingStatus.PaymentReview => "ฝ่ายบัญชีตรวจเงิน",
        BookingStatus.Pending => "โรงแรมยืนยันห้อง",
        BookingStatus.Confirmed => "เช็กอิน",
        BookingStatus.CheckedIn => "เช็กเอาต์",
        _ => null
    };

    // Each step goes through the same code as the real staff action (ledger, hotel receipt at check-in, notifications);
    // only the hotel-team permission, the stay-date check and the "hotel has a team" check are skipped. Every event says so.
    public async Task<string> SimulateNextStepAsync(int customerId, int orderId)
    {
        var order = await store.PeekOrderAsync(orderId) ?? throw new ArgumentException("ไม่พบรายการจอง");
        if (order.UserId != customerId) throw new ArgumentException("ไม่พบรายการจอง");
        switch (order.Status)
        {
            case BookingStatus.PaymentReview:
                await payments.ApprovePaymentAsync(BookingText.SystemActorId, orderId, simulated: true);
                return "ฝ่ายบัญชีตรวจการชำระเงินแล้ว ส่งต่อให้โรงแรม";
            case BookingStatus.Pending:
                await hotelDesk.DecideAsync(BookingText.SystemActorId, orderId, true, simulated: true);
                return "โรงแรมยืนยันห้องแล้ว";
            case BookingStatus.Confirmed:
                await hotelDesk.AdvanceAsync(BookingText.SystemActorId, orderId, BookingStatus.CheckedIn, simulated: true);
                return "เช็กอินแล้ว";
            case BookingStatus.CheckedIn:
                await hotelDesk.AdvanceAsync(BookingText.SystemActorId, orderId, BookingStatus.Completed, simulated: true);
                return "เช็กเอาต์แล้ว เข้าพักเสร็จสิ้น เขียนรีวิวได้เลย";
            default:
                throw new ArgumentException("รายการนี้ไม่มีขั้นถัดไปแล้ว");
        }
    }
}
