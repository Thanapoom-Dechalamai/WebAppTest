using System.ComponentModel.DataAnnotations;
namespace Nontiwat_7362_L002.ViewModels;

public class ChangePasswordForm
{
    [Required(ErrorMessage = "กรอกรหัสผ่านปัจจุบัน"), DataType(DataType.Password), Display(Name = "รหัสผ่านปัจจุบัน")]
    public string Current { get; set; } = "";

    [Required(ErrorMessage = "กรอกรหัสผ่านใหม่"), MinLength(6, ErrorMessage = "รหัสผ่านใหม่ต้องมีอย่างน้อย 6 ตัวอักษร"), MaxLength(100),
     DataType(DataType.Password), Display(Name = "รหัสผ่านใหม่")]
    public string Next { get; set; } = "";

    [Required(ErrorMessage = "ยืนยันรหัสผ่านใหม่"), Compare(nameof(Next), ErrorMessage = "รหัสผ่านใหม่สองช่องไม่ตรงกัน"),
     DataType(DataType.Password), Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string Confirm { get; set; } = "";
}
