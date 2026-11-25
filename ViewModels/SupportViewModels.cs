using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.ViewModels;

public class NewCaseForm
{
    public int? OrderId { get; set; }
    [StringLength(32)] public string? HotelId { get; set; }
    [StringLength(32)] public string? RoomType { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    [Range(1, 5)] public int? Rooms { get; set; }
    [Range(1, 10)] public int? Guests { get; set; }
    [StringLength(40)] public string? PromotionCode { get; set; }
    [StringLength(300)] public string? SpecialRequest { get; set; }
    public IFormFile? Attachment { get; set; }
    [Required(ErrorMessage = "เลือกเรื่องที่ต้องการแจ้ง")] public string Topic { get; set; } = SupportTopic.Booking;
    [Required(ErrorMessage = "กรอกหัวข้อสั้น ๆ"), StringLength(120, ErrorMessage = "หัวข้อยาวได้ไม่เกิน 120 ตัวอักษร")]
    public string Subject { get; set; } = "";
    [StringLength(2000, ErrorMessage = "รายละเอียดยาวได้ไม่เกิน 2,000 ตัวอักษร")]
    public string? Body { get; set; }
}

// The booking a case is about, as the page shows it.
public record SupportOrderInfo(int Id, string HotelName, DateTime CheckIn, DateTime CheckOut, string Status, decimal Total);

public record NewCasePage(NewCaseForm Form, SupportOrderInfo? Order, string? HotelName = null, string? RoomName = null);

public record SupportCaseRow(SupportCase Case, string CustomerName, string CustomerEmail, int Messages);

public record SupportMessageView(SupportMessage Message, string AuthorName);

public record SupportCaseView(SupportCase Case, string CustomerName, string CustomerEmail, SupportOrderInfo? Order,
    IReadOnlyList<SupportMessageView> Messages)
{
    public IReadOnlyList<SupportCaseRow> Inbox { get; init; } = [];
    public IReadOnlyList<SupportQuote> Quotes { get; init; } = []; // newest first
}

// The customer's answer to a quote: how to pay (Transfer needs a reference and/or a slip, checked by PaymentRules).
public class QuoteAcceptForm
{
    public int QuoteId { get; set; }
    public int CaseId { get; set; }
    [RegularExpression("^(Credit|Transfer)$")] public string PaymentMethod { get; set; } = "Credit";
    [StringLength(64, ErrorMessage = "เลขที่รายการยาวได้ไม่เกิน 64 ตัวอักษร")] public string? PaymentRef { get; set; }
    public IFormFile? Slip { get; set; }
}

// ---------- Ticket page for Staff/Admin (/Staff/Cases, 2026-11-25) ----------

// One line in the ticket list. Preview/LastAtUtc are the newest message; Unread follows SupportTicketRules.UnreadCount.
public record SupportTicketRow(SupportCase Case, string Number, string CustomerName, string CustomerEmail, string Initials,
    string Preview, DateTime LastAtUtc, int Unread, string? AssigneeName);

public record SupportViewCount(string View, string Label, int Count);

public record SupportOwnerOption(int Id, string Name, string Role);

public record SupportAssignmentView(SupportAssignment Row, string? ActorName, string? FromName, string? ToName);

// The open ticket: the conversation plus the right-hand panel. Assignee is null when nobody owns it; when the owner
// was later suspended or moved out of Staff/Admin, Assignee.CanOwn is false and the page flags it.
public record SupportTicketDetail(SupportCaseView Thread, string Number, Nontiwat_7362_L002.Services.Support.SupportOwner? Assignee,
    IReadOnlyList<SupportOwnerOption> Owners, IReadOnlyList<SupportTicketRow> OtherTickets, IReadOnlyList<SupportAssignmentView> History);

public record SupportInboxPage(int ActorId, string View, string? Query, IReadOnlyList<SupportViewCount> Views,
    IReadOnlyList<SupportTicketRow> Tickets, SupportTicketDetail? Selected);

