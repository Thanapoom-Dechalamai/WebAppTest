namespace Nontiwat_7362_L002.Models;

// A support ticket: a conversation between a customer and Lodgo support. Staff see it as TK000123
// (SupportTicketRules.Number); one Staff or Admin can own it (AssigneeUserId, null = nobody yet).
public class SupportCase
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public int CustomerId { get; set; }
    public string? HotelId { get; set; }
    public string? RoomType { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int? Rooms { get; set; }
    public int? Guests { get; set; }
    public string? PromotionCode { get; set; }
    public string? SpecialRequest { get; set; }
    public string Topic { get; set; } = SupportTopic.Other;
    public string Subject { get; set; } = "";
    public string Status { get; set; } = SupportStatus.Open;
    public int? AssigneeUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SupportMessage
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = "";
    public string? Attachment { get; set; }
    public bool FromStaff { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

// One change of who owns a ticket (support_assignments). User columns become null when that user is deleted.
public class SupportAssignment
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int? ActorUserId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToUserId { get; set; }
    public string Kind { get; set; } = SupportAssignmentKind.Assign;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public static class SupportAssignmentKind
{
    public const string Claim = "Claim";          // took the ticket themselves
    public const string Assign = "Assign";        // gave it to someone else
    public const string Release = "Release";      // left it with nobody
    public const string AutoClaim = "AutoClaim";  // first support reply on a ticket nobody owned
    public const string Suspended = "Suspended";  // the owner's account was suspended: released by the system
    public const string RoleChanged = "RoleChanged"; // the owner is no longer Staff/Admin: released by the system
    public static string Label(string kind) => kind switch
    {
        Claim => "รับเรื่อง",
        Assign => "มอบหมาย",
        Release => "ปล่อยเรื่อง",
        AutoClaim => "รับเรื่องเมื่อตอบครั้งแรก",
        Suspended => "ปล่อยคืนเพราะบัญชีถูกระงับ",
        RoleChanged => "ปล่อยคืนเพราะเปลี่ยนบทบาท",
        _ => kind
    };
}

// Open → Answered (support replied) → Closed. A customer reply sends it back to Open.
public static class SupportStatus
{
    public const string Open = "Open";
    public const string Answered = "Answered";
    public const string Closed = "Closed";
    public static readonly string[] All = [Open, Answered, Closed];
    public static string Label(string status) => status switch
    {
        Open => "รอ support ตอบ",
        Answered => "support ตอบแล้ว",
        Closed => "ปิดเรื่องแล้ว",
        _ => status
    };
}

public static class SupportTopic
{
    public const string Booking = "Booking";
    public const string Payment = "Payment";
    public const string Stay = "Stay";
    public const string Other = "Other";
    public static readonly string[] All = [Booking, Payment, Stay, Other];
    public static string Label(string topic) => topic switch
    {
        Booking => "การจอง",
        Payment => "การชำระเงินหรือเครดิต",
        Stay => "ที่พัก",
        _ => "อื่นๆ"
    };
}

// A price offer support sends inside a ticket (support_quotes, 2026-11-25). Nothing is charged until the customer
// accepts it and picks a payment method; then CheckoutService books it with exactly these values.
public class SupportQuote
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int CustomerId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString(); // the booking's request id: a double accept books once
    public string HotelId { get; set; } = "";
    public string HotelName { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string RoomName { get; set; } = "";
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Rooms { get; set; }
    public int Guests { get; set; }
    public string? PromotionCode { get; set; }
    public string? SpecialRequest { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = QuoteStatus.Open;
    public DateTime ExpiresAtUtc { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
}

// Open → Accepted (booked) / Declined (customer said no) / Replaced (support sent a newer one).
// "Expired" is never stored: an Open quote past ExpiresAtUtc reads as expired (QuoteRules.StateOf).
public static class QuoteStatus
{
    public const string Open = "Open";
    public const string Accepted = "Accepted";
    public const string Declined = "Declined";
    public const string Replaced = "Replaced";
    public const string Expired = "Expired";
    public static string Label(string status) => status switch
    {
        Open => "รอคุณยืนยัน",
        Accepted => "ยืนยันแล้ว",
        Declined => "ไม่รับ",
        Replaced => "มีใบใหม่แทน",
        Expired => "หมดอายุ",
        _ => status
    };
}
