namespace Nontiwat_7362_L002.Models;
public class Promotion
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string DiscountKind { get; set; } = "Percent";
    public decimal DiscountValue { get; set; }
    public int MinNights { get; set; } = 1;
    public decimal MinSpend { get; set; }
    public decimal? MaxDiscount { get; set; }
    public string Terms => Code is null ? "ข่าวสารจาก Lodgo" :
        (DiscountKind=="Percent" ? $"ลด {DiscountValue:0.##}%" : $"ลด ฿{DiscountValue:N0}") +
        (MaxDiscount is > 0 ? $" สูงสุด ฿{MaxDiscount:N0}" : "") +
        $" · ขั้นต่ำ {MinNights} คืน / ฿{MinSpend:N0} · 1 ครั้งต่อบัญชี";
    public string Title { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public bool IsActive { get; set; } = true;
    public int UpdatedByUserId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
