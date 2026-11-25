using System.ComponentModel.DataAnnotations;

namespace Nontiwat_7362_L002.ViewModels;

public class AddUserViewModel : RegisterViewModel, IHotelPickerForm
{
    public List<string> HotelIds { get; set; } = [];
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public IReadOnlyList<HotelOption> HotelOptions { get; set; } = [];

    [Required(ErrorMessage = "กรุณาเลือกสิทธิ์ผู้ใช้")]
    [RegularExpression(Nontiwat_7362_L002.Security.AppRoles.Pattern, ErrorMessage = "สิทธิ์ผู้ใช้ไม่ถูกต้อง")]
    [Display(Name = "สิทธิ์ผู้ใช้")]
    public string Role { get; set; } = "User";
}
