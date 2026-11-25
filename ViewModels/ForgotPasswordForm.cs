using System.ComponentModel.DataAnnotations;

namespace Nontiwat_7362_L002.ViewModels;

public class ForgotPasswordForm
{
    [Required(ErrorMessage = "กรอกอีเมลที่ใช้สมัคร"), EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง"), StringLength(254), Display(Name = "อีเมล")]
    public string Email { get; set; } = "";
}
