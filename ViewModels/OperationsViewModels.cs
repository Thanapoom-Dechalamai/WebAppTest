using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels;

// ---------- Room calendar (HotelStaff) ----------
public class AllotmentForm
{
    [Required] public string HotelId { get; set; } = "";
    [Required] public string RoomType { get; set; } = "";
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }
    // "rooms" = set how many rooms are sold, "close" = stop selling, "open" = sell again.
    [Required] public string Action { get; set; } = "rooms";
    public int? Rooms { get; set; }
}

public record CalendarRow(string RoomType, string RoomName, IReadOnlyList<Nontiwat_7362_L002.Services.AvailabilityService.Night> Nights);

public record CalendarPage(IReadOnlyList<HotelCardViewModel> Hotels, HotelCardViewModel? Hotel, DateTime Month,
    IReadOnlyList<CalendarRow> Rows, bool IsAdmin)
{
    public AllotmentForm Form { get; init; } = new();
}

// ---------- Booking history and notifications ----------
public record TimelineItem(DateTime AtUtc, string Status, string Note, string Actor)
{
    public string Title => Status switch
    {
        BookingStatus.Pending => "ส่งคำขอจอง", BookingStatus.Confirmed => "โรงแรมยืนยันการจอง",
        BookingStatus.CheckedIn => "เช็กอิน", BookingStatus.Completed => "เช็กเอาต์ · เข้าพักเสร็จ",
        BookingStatus.NoShow => "บันทึกว่าไม่มาเข้าพัก", BookingStatus.Cancelled => "ยกเลิกและคืนเครดิต", _ => Status
    };
}

public record NotificationListPage(IReadOnlyList<Notification> Items, int Unread, bool UnreadOnly);

// ---------- Audit log (Admin) ----------
public record AuditLogRow(AuditLog Log, string? ActorName, string? ActorEmail)
{
    public string TableLabel => AuditLabels.Table(Log.TableName);
    public string ActionLabel => Log.Action switch { "Added" => "เพิ่ม", "Modified" => "แก้ไข", "Deleted" => "ลบ", _ => Log.Action };

    // Field-by-field changes: for an edit only the fields that changed, for an add/delete every field.
    public IReadOnlyList<(string Field, string? Old, string? New)> Changes()
    {
        var before = Parse(Log.OldData);
        var after = Parse(Log.NewData);
        var fields = before.Keys.Union(after.Keys).ToList();
        var list = new List<(string, string?, string?)>();
        foreach (var field in fields)
        {
            before.TryGetValue(field, out var oldValue);
            after.TryGetValue(field, out var newValue);
            if (Log.Action == "Modified" && oldValue == newValue) continue;
            list.Add((field, oldValue, newValue));
        }
        return list;
    }

    private static Dictionary<string, string?> Parse(string? json)
    {
        var values = new Dictionary<string, string?>();
        if (string.IsNullOrWhiteSpace(json)) return values;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return values;
            foreach (var property in document.RootElement.EnumerateObject())
                values[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.GetRawText()
                };
        }
        catch (JsonException) { }
        return values;
    }
}

public record AuditLogPage(IReadOnlyList<AuditLogRow> Rows, int Total, int PageNumber, int PageSize,
    IReadOnlyList<string> Tables, string? Table, string? Kind, string? Actor, string? Record, DateTime? From, DateTime? To)
{
    public int Pages => Math.Max(1, (Total + PageSize - 1) / PageSize);
}

public static class AuditLabels
{
    private static readonly Dictionary<string, string> Tables = new()
    {
        ["stay_orders"] = "การจอง", ["wallet_entries"] = "รายการเครดิต", ["member_wallets"] = "กระเป๋าเครดิต",
        ["users"] = "บัญชีผู้ใช้", ["hotel_assignments"] = "ทีมโรงแรม", ["hotel_decisions"] = "การตัดสินใจของโรงแรม",
        ["hotel_owner_entries"] = "ยอดรับโรงแรม", ["hotel_payouts"] = "โอนเงินให้โรงแรม", ["promotions"] = "โปรโมชั่น",
        ["promotion_redemptions"] = "การใช้โค้ด", ["cart_items"] = "Cart", ["hotel_reviews"] = "รีวิว",
        ["booking_events"] = "ประวัติการจอง", ["room_allotments"] = "ปฏิทินห้องว่าง", ["hotels"] = "โรงแรม",
        ["room_types"] = "ประเภทห้อง", ["stay_environments"] = "ข้อมูลบรรยากาศ", ["hotel_theme_tags"] = "แท็กโรงแรม"
    };
    public static string Table(string name) => Tables.TryGetValue(name, out var label) ? label : name;
}
