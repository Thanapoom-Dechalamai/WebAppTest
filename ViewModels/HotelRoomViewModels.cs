using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;

namespace Nontiwat_7362_L002.ViewModels;

public class HotelRoomForm : IValidatableObject
{
    [Display(Name = "ชื่อประเภทห้อง"), Required(ErrorMessage = "กรอกชื่อประเภทห้อง"), StringLength(100)]
    public string Name { get; set; } = "";

    [Display(Name = "ราคาต่อห้องต่อคืน (บาท)")]
    public decimal Price { get; set; }

    [Display(Name = "จำนวนห้องตามปกติ")]
    [Range(0, RoomCapacity.Max, ErrorMessage = "จำนวนห้องต้องอยู่ระหว่าง 0–500")]
    public int Capacity { get; set; }

    [Display(Name = "ขนาดห้อง (ตร.ม.)"), Range(1, int.MaxValue)]
    public int? SizeSquareMeters { get; set; }

    [Display(Name = "ประเภทเตียง"), StringLength(100)]
    public string? BedType { get; set; }

    [Display(Name = "จำนวนเตียง"), Range(1, 20, ErrorMessage = "จำนวนเตียงต้องอยู่ระหว่าง 1–20")]
    public int? BedCount { get; set; }

    [Display(Name = "รายละเอียดและสิ่งอำนวยความสะดวกในห้อง"), StringLength(600)]
    public string? Features { get; set; }

    [Display(Name = "เปิดขายห้องประเภทนี้")]
    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!HotelRoomRules.RateValid(Price))
            yield return new ValidationResult($"กรอกราคา 1–{decimal.Floor(HotelRoomRules.MaxRate * 100) / 100:N2} บาท ทศนิยมไม่เกิน 2 ตำแหน่ง", [nameof(Price)]);
    }
}

public sealed record HotelRoomList(HotelData Hotel, IReadOnlyList<RoomOption> Rooms);
