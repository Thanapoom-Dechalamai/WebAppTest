namespace Nontiwat_7362_L002.ViewModels;

public record GuideStep(int No, string Title, string Role, string What, string Email, string Password, string TryUrl);
public record GuideAbility(string Text, string Url);
public record RoleAbility(string Role, string Email, string Password, IReadOnlyList<GuideAbility> Abilities);

// Text of the "Lodgo ทำอะไรได้บ้าง" page (/Home/Guide, Owner and Admin only). Demo accounts match Database/DEMO-ACCOUNTS.md.
public static class GuideContent
{
    private const string Customer = "customer@lodgo.test", Hotel = "hotelowner@lodgo.test", Easy = "123456", HotelPassword = "LodgoDemo!2026";

    public static readonly IReadOnlyList<GuideStep> Steps =
    [
        new(1, "ลูกค้าส่งคำขอจอง", "Customer · ลูกค้า", "เลือกที่พัก ห้อง และวันเช็กอินตั้งแต่วันพรุ่งนี้ กด “ติดต่อเจ้าหน้าที่เพื่อจอง” แล้วส่งคำขอในแชต", Customer, Easy, "/Account/Index"),
        new(2, "เจ้าหน้าที่ส่งใบเสนอราคา", "Staff · Lodgo support", "เปิด ticket ของลูกค้า ตรวจราคาและห้องว่าง แล้วส่งใบเสนอราคาในแชต ระบบยังไม่กันห้องหรือหักเครดิต", "staff@lodgo.test", Easy, "/Staff/Cases"),
        new(3, "ลูกค้ายืนยันและชำระ", "Customer · ลูกค้า", "เปิดแชต ดูยอดและรายละเอียด เลือกจ่ายด้วยเครดิตหรือโอนเงิน แล้วกดยืนยันก่อนหมดอายุ หรือกดไม่รับใบเสนอราคา เมื่อยืนยันสำเร็จจึงได้เลขการจอง", Customer, Easy, "/Support"),
        new(4, "บัญชีตรวจการชำระเงิน", "AC · บัญชี", "ทุกการจองมาที่หน้า “ตรวจการชำระเงิน” ก่อน ตรวจยอดเครดิตหรือหลักฐานโอนเงิน แล้วกด “ยอดถูกต้อง · ส่งให้โรงแรม” ถ้าไม่ผ่าน รายการถูกยกเลิกและระบบคืนยอดที่รับชำระแล้วเป็นเครดิต", "ac@lodgo.test", Easy, "/AC/Payments"),
        new(5, "โรงแรมตรวจห้องว่างและยืนยัน", "HotelStaff · พนักงานโรงแรม", "เปิดคิว “รอตรวจห้องว่างและยืนยัน” แล้วกดยืนยัน ลูกค้าได้รับแจ้งเตือนทันที", Hotel, HotelPassword, "/HotelOwner/Index?status=Pending"),
        new(6, "เช็กอินและเช็กเอาต์", "HotelStaff · พนักงานโรงแรม", "เปิดหน้าแขกวันนี้ กดเช็กอินเมื่อแขกมาถึง และเช็กเอาต์เมื่อแขกออก", Hotel, HotelPassword, "/HotelOwner/Today"),
        new(7, "ลูกค้ารีวิว · โรงแรมตอบรีวิว", "Customer · ลูกค้า แล้ว HotelStaff", "ลูกค้ากด “เขียนรีวิว” ในหน้าที่พักหรือการจองที่เข้าพักเสร็จแล้ว และให้คะแนน 5 ด้าน จากนั้นโรงแรมตอบได้ที่หน้า “ตอบรีวิว” คำตอบแสดงใต้รีวิวและลูกค้าได้รับแจ้งเตือน", Customer, Easy, "/Stays/Bookings"),
        new(8, "แจ้งปัญหา · Support ช่วยลูกค้า", "Customer แล้ว Staff · Lodgo support", "ลูกค้ากด “แชตกับเจ้าหน้าที่เรื่องการจองนี้” เพื่อขอแก้ไขหรือยกเลิก เจ้าหน้าที่ตรวจเงื่อนไข ดำเนินการ และแจ้งผลในแชต", "staff@lodgo.test", Easy, "/Staff/Cases"),
        new(9, "บัญชีสรุปยอดและโอนให้โรงแรม", "AC · บัญชี", "ดูว่าเงินแต่ละบาทมาจากการจองไหน หักค่าคอมมิชชัน บันทึกการโอนให้โรงแรม และดาวน์โหลด CSV", "ac@lodgo.test", Easy, "/AC/Settlement"),
    ];

    public static readonly IReadOnlyList<RoleAbility> Roles =
    [
        new("Customer · ลูกค้า", Customer, Easy, [
            new("ค้นหาที่พักและส่งคำขอจอง", "/Account/Index"), new("ดูใบเสนอราคาและยืนยันการจองในแชต", "/Support"),
            new("ดูสถานะและประวัติการจอง", "/Stays/Bookings"), new("เติมเครดิตและดูประวัติเงิน", "/Member/Wallet"),
            new("โปรโมชั่นที่ใช้ได้", "/Customer/Promotions"), new("การแจ้งเตือน", "/Notifications/Index"),
            new("รายการโปรด (กดหัวใจบนการ์ดที่พัก)", "/Wishlist"), new("แจ้งปัญหาถึง Lodgo support", "/Support"),
            new("พิมพ์ใบยืนยันการจอง (ในหน้าการจองแต่ละรายการ)", "/Stays/Bookings")]),
        new("HotelStaff · พนักงานโรงแรม", Hotel, HotelPassword, [
            new("ยืนยันหรือปฏิเสธคำขอจอง", "/HotelOwner/Index?status=Pending"), new("แขกวันนี้: เช็กอิน / เช็กเอาต์", "/HotelOwner/Today"),
            new("ปฏิทินห้องว่าง ตั้งจำนวนห้องและปิดรับจอง", "/HotelOwner/Calendar"), new("ตอบรีวิวของผู้เข้าพัก", "/HotelOwner/Reviews?filter=unanswered"),
            new("ยอดรับของโรงแรม", "/HotelOwner/Index")]),
        new("Staff · Lodgo support", "staff@lodgo.test", Easy, [
            new("ตอบ ticket ส่งใบเสนอราคา และช่วยแก้ไขการจอง", "/Staff/Cases"), new("ค้นหาใบจองของลูกค้า", "/Staff/Index"),
            new("คำขอค้างเกิน 24 ชั่วโมง", "/Staff/Index?status=Stale")]),
        new("Owner · เจ้าของกิจการ", "owner@lodgo.test", Easy, [new("แดชบอร์ด กราฟ 12 เดือน และสัญญาณเตือน", "/Owner/Index")]),
        new("Manager · ผู้จัดการ", "manager@lodgo.test", Easy, [
            new("จัดการบัญชีทีมงาน", "/Manager/Index"), new("เพิ่มพนักงาน", "/Manager/AddEmployee"), new("จัดการโปรโมชั่น", "/Manager/Promotions"),
            new("KPI การจอง: อัตรายกเลิก เวลายืนยัน โปรโมชั่น", "/Manager/Kpi")]),
        new("AC · บัญชี", "ac@lodgo.test", Easy, [
            new("ตรวจการชำระเงินของทุกการจอง ก่อนส่งให้โรงแรม", "/AC/Payments"), new("เงินของลูกค้าอยู่ตรงไหน", "/AC/Index"), new("สรุปยอดและโอนให้โรงแรม", "/AC/Settlement"), new("กระทบยอดเครดิต", "/AC/Reconcile"),
            new("ดาวน์โหลด CSV รายการเงินและสรุปรายโรงแรม", "/AC/Index")]),
        new("Admin · ผู้ดูแลระบบ", "admin@lodgo.test", Easy, [
            new("ผู้ใช้: ระงับบัญชี / รีเซ็ตรหัสผ่าน", "/Admin/UserList"), new("ทีมพนักงานโรงแรม", "/HotelManagement/Index"),
            new("ประวัติการใช้งานระบบ", "/Admin/AuditLogs"), new("ปฏิทินห้องว่างทุกโรงแรม", "/HotelOwner/Calendar")]),
    ];

    // "ลองเลย": sign in as the step's demo account (switching if someone is signed in) and land on the step's page.
    public static string LoginUrl(GuideStep step) => LoginUrl(step.Email, step.TryUrl);
    public static string LoginUrl(string email, string returnUrl) =>
        $"/Account/Login?switchAccount=true&email={Uri.EscapeDataString(email)}&returnUrl={Uri.EscapeDataString(returnUrl)}";
}
