using System.ComponentModel.DataAnnotations;

namespace Nontiwat_7362_L002.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(254)]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    [StringLength(100)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
