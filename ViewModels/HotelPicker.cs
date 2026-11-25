using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nontiwat_7362_L002.ViewModels;

public record HotelOption(string Id, string Name, string City);

// The add/edit user forms let Admin tick the hotels a HotelStaff account looks after.
public interface IHotelPickerForm
{
    string Role { get; }
    List<string> HotelIds { get; set; }
    IReadOnlyList<HotelOption> HotelOptions { get; set; }
}

public static class HotelPicker
{
    public const string RoleOnlyMessage = "เลือกโรงแรมที่ดูแลได้เฉพาะบทบาทพนักงานโรงแรม";
    public const string NoHotelWarning = "ยังไม่ได้ผูกโรงแรม พนักงานคนนี้จะยังไม่เห็นการจองจนกว่าจะเลือกโรงแรมที่ดูแล";
}
