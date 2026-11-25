using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;

namespace Nontiwat_7362_L002.ViewModels;

public sealed class HotelPhotoForm : IValidatableObject
{
    [Display(Name = "URL รูปภาพ"), StringLength(1000)]
    public string? Url { get; set; }

    [Display(Name = "ไฟล์รูปภาพ")]
    public IFormFile? Upload { get; set; }

    [Display(Name = "เครดิตภาพ"), StringLength(200)]
    public string? Credit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var url = Url?.Trim();
        var hasUrl = !string.IsNullOrEmpty(url);
        if (hasUrl == (Upload is not null))
            yield return new ValidationResult("เลือก URL หรืออัปโหลดไฟล์อย่างใดอย่างหนึ่ง", [nameof(Url), nameof(Upload)]);
        if (hasUrl && (!HotelCatalogRules.IsPhotoUrlValid(url) || HotelPhotoService.IsUploadedUrl(url!)))
            yield return new ValidationResult("กรอก URL รูปที่ขึ้นต้นด้วย https:// หรือ http://", [nameof(Url)]);
    }
}

public sealed record HotelPhotoPage(HotelData Hotel, RoomOption? Room, IReadOnlyList<HotelGalleryPhoto> Photos, HotelPhotoForm Form);
