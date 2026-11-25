using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Services;

// Numbers for the "งานของคุณวันนี้" panel. Each role gets 2–5 boxes, each linking to where the work is done.
// Only counts and sums: this runs on every visit to a role's home page.
public class TodayService(ApplicationDbContext db, SettlementService settlement, WishlistService wishlist, SupportService support, ReviewReplyService replies,
    OwnerInsights insights, KpiService kpi, IThaiClock clock)
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(24);
    // Midnight today in Thailand, expressed in UTC (the database stores UTC).
    private DateTime ThaiMidnightUtc => clock.Today.Add(-ThaiClock.Offset);

    public async Task<IReadOnlyList<TodayTile>> ForAsync(ClaimsPrincipal user)
    {
        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return [];
        return AppRoles.Current(user) switch
        {
            AppRoles.HotelOwner => await HotelStaffAsync(id),
            AppRoles.Staff => await SupportAsync(),
            AppRoles.AC => await AccountsAsync(),
            AppRoles.Manager => await ManagerAsync(),
            AppRoles.Owner => await OwnerAsync(),
            AppRoles.Admin => await AdminAsync(),
            _ => await CustomerAsync(id)
        };
    }

    private async Task<IReadOnlyList<TodayTile>> CustomerAsync(int id)
    {
        var today = clock.Today;
        var upcoming = await db.Orders.CountAsync(x => x.UserId == id && x.CheckIn >= today
            && (x.Status == BookingStatus.PaymentReview || x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed));
        var unread = await db.Notifications.CountAsync(x => x.UserId == id && !x.IsRead);
        return [
            TodayTile.Count("การเดินทางที่จะถึง", upcoming, "/Stays/Bookings", "ดูรายละเอียดและสถานะ", needsAction: false),
            TodayTile.Count("การแจ้งเตือนที่ยังไม่อ่าน", unread, "/Notifications/Index?unread=true", "มีอัปเดตเรื่องการจอง"),
            TodayTile.Count("support ตอบแล้ว รอคุณอ่าน", await support.WaitingCountAsync(id), "/Support", "เรื่องที่แจ้ง Lodgo support"),
            TodayTile.Count("รายการโปรด", await wishlist.CountAsync(id), "/Wishlist", "ที่พักที่กดหัวใจเก็บไว้", needsAction: false)
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> HotelStaffAsync(int id)
    {
        var today = clock.Today;
        var hotels = db.HotelAssignments.Where(x => x.OwnerUserId == id).Select(x => x.HotelId);
        var mine = db.Orders.Where(x => hotels.Contains(x.HotelId));
        return [
            TodayTile.Count("คำขอรอยืนยัน", await mine.CountAsync(x => x.Status == BookingStatus.Pending), "/HotelOwner/Index?status=Pending", "ตรวจห้องว่างแล้วยืนยัน"),
            TodayTile.Count("แขกเช็กอินวันนี้", await mine.CountAsync(x => x.Status == BookingStatus.Confirmed && x.CheckIn == today), "/HotelOwner/Today", "รอเช็กอินที่หน้าแขกวันนี้"),
            TodayTile.Count("ครบกำหนดเช็กเอาต์", await mine.CountAsync(x => x.Status == BookingStatus.CheckedIn && x.CheckOut <= today), "/HotelOwner/Today", "เช็กเอาต์ให้แขก"),
            TodayTile.Count("กำลังเข้าพัก", await mine.CountAsync(x => x.Status == BookingStatus.CheckedIn), "/HotelOwner/Today", "แขกที่อยู่ในโรงแรมตอนนี้", needsAction: false),
            TodayTile.Count("รีวิวที่ยังไม่ตอบ", await replies.UnansweredCountAsync(id), "/HotelOwner/Reviews?filter=unanswered", "รีวิวจากผู้เข้าพักจริง")
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> SupportAsync()
    {
        var staleBefore = DateTime.UtcNow - StaleAfter;
        return [
            TodayTile.Count("คำขอค้างเกิน 24 ชม.", await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending && (x.HotelSentAtUtc ?? x.CreatedAtUtc) < staleBefore), "/Staff/Index?status=Stale", "ติดตามโรงแรมหรือช่วยลูกค้า"),
            TodayTile.Count("รอโรงแรมยืนยันทั้งหมด", await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending), "/Staff/Index?status=Pending", "คำขอที่ยังเปิดอยู่", needsAction: false),
            TodayTile.Count("เรื่องที่ลูกค้าแจ้งและยังเปิด", await support.OpenCountAsync(), "/Staff/Cases?status=Open", "อ่านแล้วตอบลูกค้า")
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> AccountsAsync()
    {
        var owed = (await settlement.RowsAsync(null)).Where(x => x.Outstanding > 0).ToList();
        return [
            TodayTile.Count("การจองรอตรวจเงิน", await db.Orders.CountAsync(x => x.Status == BookingStatus.PaymentReview), "/AC/Payments", "ตรวจยอดก่อนส่งให้โรงแรม"),
            TodayTile.Count("โรงแรมที่ค้างโอน", owed.Count, "/AC/Settlement", "สรุปยอดและบันทึกการโอน"),
            TodayTile.Money("ยอดค้างโอนรวม", owed.Sum(x => x.Outstanding), "/AC/Settlement", "หลังหักค่าคอมมิชชันแล้ว")
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> ManagerAsync()
    {
        var staleBefore = DateTime.UtcNow - StaleAfter;
        var since30 = DateTime.UtcNow.AddDays(-30);
        var recent = await db.Orders.CountAsync(x => x.CreatedAtUtc >= since30);
        var cancelled = await db.Orders.CountAsync(x => x.CreatedAtUtc >= since30 && x.Status == BookingStatus.Cancelled);
        var rate = recent == 0 ? "—" : (cancelled * 100.0 / recent).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
        return [
            TodayTile.Count("คำขอสิทธิ์ดูแลโรงแรม", await db.HotelAccessRequests.CountAsync(x => x.Status == AccessRequestStatus.Pending), "/HotelRequests", "ตรวจหลักฐานแล้วอนุมัติ"),
            TodayTile.Count("คำขอค้างเกิน 24 ชม.", await db.Orders.CountAsync(x => x.Status == BookingStatus.Pending && (x.HotelSentAtUtc ?? x.CreatedAtUtc) < staleBefore), "/Manager/Index", "ให้ Lodgo support ติดตามโรงแรม"),
            TodayTile.Info("อัตรายกเลิก 30 วัน", rate, $"ยกเลิก {cancelled} จาก {recent} การจอง", "/Manager/Index"),
            TodayTile.Count("การจองใหม่ 7 วัน", await db.Orders.CountAsync(x => x.CreatedAtUtc >= DateTime.UtcNow.AddDays(-7)), "/Manager/Index", "ทุกโรงแรม", needsAction: false),
            TodayTile.Info("เวลายืนยันเฉลี่ย 30 วัน", KpiService.Hours(await kpi.AvgConfirmHoursAsync(30)), "จากจองถึงโรงแรมยืนยัน", "/Manager/Kpi")
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> OwnerAsync()
    {
        var todayStart = ThaiMidnightUtc;
        var monthStart = new DateTime(clock.Today.Year, clock.Today.Month, 1).AddHours(-7);
        var monthEntries = db.WalletEntries.Where(x => x.CreatedAtUtc >= monthStart);
        // Payments are stored as negative wallet amounts, refunds as positive.
        var paid = -(await monthEntries.Where(x => x.Kind == "Payment").SumAsync(x => (decimal?)x.Amount) ?? 0);
        var refunded = await monthEntries.Where(x => x.Kind == "Refund").SumAsync(x => (decimal?)x.Amount) ?? 0;
        return [
            TodayTile.Count("การจองวันนี้", await db.Orders.CountAsync(x => x.CreatedAtUtc >= todayStart), "/Owner/Index", "ตั้งแต่เที่ยงคืน", needsAction: false),
            TodayTile.Info("รายได้สุทธิเดือนนี้", "฿" + (paid - refunded).ToString("N0", System.Globalization.CultureInfo.InvariantCulture), "ยอดชำระหักคืนเครดิต", "/Owner/Index"),
            TodayTile.Count("สัญญาณเตือน", await insights.WarningCountAsync(), "/Owner/Index#warnings", "ยกเลิกสูง · รีวิวต่ำ · คำขอค้าง")
        ];
    }

    private async Task<IReadOnlyList<TodayTile>> AdminAsync()
    {
        var todayStart = ThaiMidnightUtc;
        var today = clock.Today.ToString("yyyy-MM-dd");
        return [
            TodayTile.Count("ผู้ใช้ทั้งหมด", await db.Users.CountAsync(), "/Admin/UserList", "ทุก role", needsAction: false),
            TodayTile.Count("คำขอสิทธิ์ดูแลโรงแรม", await db.HotelAccessRequests.CountAsync(x => x.Status == AccessRequestStatus.Pending), "/HotelRequests", "ตรวจหลักฐานแล้วอนุมัติ"),
            TodayTile.Count("บัญชีที่ถูกระงับ", await db.Users.CountAsync(x => x.IsSuspended), "/Admin/UserList", "ตรวจว่ายังต้องระงับอยู่ไหม"),
            TodayTile.Count("แก้ไขข้อมูลวันนี้", await db.AuditLogs.CountAsync(x => x.CreatedAtUtc >= todayStart), "/Admin/AuditLogs?from=" + today, "ดูว่าใครแก้อะไร", needsAction: false)
        ];
    }
}
