using System.ComponentModel.DataAnnotations;

namespace Nontiwat_7362_L002.ViewModels;

public class EditUserViewModel : IHotelPickerForm
{
    public List<string> HotelIds { get; set; } = [];
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public IReadOnlyList<HotelOption> HotelOptions { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required, StringLength(80), Display(Name = "ชื่อ")]
    public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(80), Display(Name = "นามสกุล")]
    public string LastName { get; set; } = string.Empty;
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(254, ErrorMessage = "อีเมลต้องไม่เกิน 254 ตัวอักษร")]
    [RegularExpression(EmailRules.Pattern, ErrorMessage = EmailRules.Message)]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;
    [Required, RegularExpression(Nontiwat_7362_L002.Security.AppRoles.Pattern), Display(Name = "สิทธิ์การใช้งาน")]
    public string Role { get; set; } = "User";
}
