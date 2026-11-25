using System.ComponentModel.DataAnnotations;

namespace Nontiwat_7362_L002.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    [StringLength(80, ErrorMessage = "ชื่อต้องไม่เกิน 80 ตัวอักษร")]
    [Display(Name = "ชื่อ")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    [StringLength(80, ErrorMessage = "นามสกุลต้องไม่เกิน 80 ตัวอักษร")]
    [Display(Name = "นามสกุล")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [RegularExpression(EmailRules.Pattern, ErrorMessage = EmailRules.Message)]
    [StringLength(254, ErrorMessage = "อีเมลต้องไม่เกิน 254 ตัวอักษร")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, MinimumLength = 6,
        ErrorMessage = "รหัสผ่านต้องมีความยาว 6-100 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
    [Compare(nameof(Password), ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
    [DataType(DataType.Password)]
    [Display(Name = "ยืนยันรหัสผ่าน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
