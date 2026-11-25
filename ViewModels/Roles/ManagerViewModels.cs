using System.ComponentModel.DataAnnotations;
namespace Nontiwat_7362_L002.ViewModels.Roles;
public class PromotionForm : IValidatableObject
{
    [RegularExpression("^[A-Za-z0-9-]{3,40}$", ErrorMessage="โค้ดใช้ A–Z, 0–9 และ - จำนวน 3–40 ตัว")]
    public string? Code {get;set;}
    [RegularExpression("^(Percent|Fixed)$")] public string DiscountKind {get;set;}="Percent";
    [Range(typeof(decimal),"0","50000")] public decimal DiscountValue {get;set;}
    [Range(1,30)] public int MinNights {get;set;}=1;
    [Range(typeof(decimal),"0","1000000")] public decimal MinSpend {get;set;}
    [Range(typeof(decimal),"0.01","50000")] public decimal? MaxDiscount {get;set;}
    [Range(0,int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(120)] public string Title { get; set; } = "";
    [Required, StringLength(1000)] public string Details { get; set; } = "";
    [DataType(DataType.Date)] public DateTime StartsOn { get; set; } = DateTime.UtcNow.AddHours(7).Date;
    [DataType(DataType.Date)] public DateTime EndsOn { get; set; } = DateTime.UtcNow.AddHours(7).Date.AddDays(30);
    public bool IsActive { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(!string.IsNullOrWhiteSpace(Code) && (DiscountValue<=0 || (DiscountKind=="Percent" && DiscountValue>100)))
            yield return new ValidationResult("ระบุส่วนลดมากกว่า 0 และเปอร์เซ็นต์ไม่เกิน 100",[nameof(DiscountValue)]);
        if (StartsOn.Year < 2000 || EndsOn.Year > 2100 || EndsOn < StartsOn)
            yield return new ValidationResult("ระบุช่วงวันที่ให้ถูกต้อง วันสิ้นสุดต้องไม่ก่อนวันเริ่ม", [nameof(EndsOn)]);
    }
}
