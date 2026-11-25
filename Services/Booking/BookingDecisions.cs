using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>Records hotel and cancellation decisions, their history, and their notifications.</summary>
public sealed class BookingDecisions(IBookingJournal journal, IActorDirectory actors, IThaiClock clock)
{
    internal Task NotifyHotelNoShowAsync(StayOrder order) =>
        journal.NotifyHotelTeamAsync(order, "ระบบปิดเป็นไม่มาเข้าพัก",
            $"{BookingText.Number(order.Id)} · เลยวันเช็กเอาต์ {ThaiDates.Short(order.CheckOut)} แล้วโดยไม่ได้เช็กอิน · ถ้าแขกมาจริงให้แจ้ง Lodgo support");

    internal async Task RecordAsync(StayOrder order, int actorId, string action, string note, bool refunded = true)
    {
        await journal.RecordDecisionAsync(order, actorId, action, note, clock.UtcNow);
        var actor = await actors.DescribeAsync(actorId);
        await journal.AddEventAsync(order, action, note, actorId == BookingText.SystemActorId ? null : actorId, actor.Label);
        var no = BookingText.Number(order.Id);
        switch (action)
        {
            case BookingStatus.Confirmed:
                await journal.NotifyCustomerAsync(order, "โรงแรมยืนยันการจองแล้ว", $"{order.HotelName} ยืนยัน {no} แล้ว · เช็กอิน {ThaiDates.Long(order.CheckIn)}");
                break;
            case BookingStatus.CheckedIn:
                await journal.NotifyCustomerAsync(order, "เช็กอินเรียบร้อย", $"ยินดีต้อนรับสู่ {order.HotelName} · {no}");
                break;
            case BookingStatus.Completed:
                await journal.NotifyCustomerAsync(order, "เข้าพักเสร็จสิ้น · รีวิวที่พักได้แล้ว", $"เล่าประสบการณ์ที่ {order.HotelName} ให้ผู้เข้าพักคนอื่นรู้ · {no}");
                break;
            case BookingStatus.NoShow:
                await journal.NotifyCustomerAsync(order, "โรงแรมบันทึกว่าไม่ได้มาเข้าพัก", $"{no} ปิดรายการโดยไม่คืนเครดิต หากไม่ถูกต้องติดต่อ Lodgo support");
                break;
            case BookingStatus.Cancelled:
                if (actorId != order.UserId)
                    await journal.NotifyCustomerAsync(order, refunded ? "การจองถูกยกเลิก · คืนเครดิตแล้ว" : "การจองถูกยกเลิก", $"{no} · {note}");
                // The hotel team hears about cancellations it did not make itself.
                if (actor.Role != AppRoles.HotelOwner && refunded)
                    await journal.NotifyHotelTeamAsync(order, actorId == order.UserId ? "ลูกค้ายกเลิกการจอง" : "การจองถูกยกเลิกโดย Lodgo",
                        $"{no} · {order.RoomName} {order.Rooms} ห้อง · {ThaiDates.Short(order.CheckIn)}–{ThaiDates.Short(order.CheckOut)} · ห้องกลับมาว่างแล้ว");
                break;
        }
    }
}
