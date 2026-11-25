using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;

namespace Nontiwat_7362_L002.ViewModels;

public sealed class HotelCatalogForm : IValidatableObject
{
    [Display(Name = "ชื่อโรงแรม")]
    [Required(ErrorMessage = "กรอกชื่อโรงแรม")]
    [StringLength(160, ErrorMessage = "ชื่อโรงแรมยาวได้ไม่เกิน 160 ตัวอักษร")]
    public string Name { get; set; } = "";

    [Display(Name = "จังหวัด")]
    [Required(ErrorMessage = "กรอกจังหวัด")]
    [StringLength(80, ErrorMessage = "ชื่อจังหวัดยาวได้ไม่เกิน 80 ตัวอักษร")]
    public string City { get; set; } = "";

    [Display(Name = "ราคาพื้นฐานต่อคืน (บาท)")]
    [Range(0, int.MaxValue, ErrorMessage = "กรอกราคาอ้างอิงเป็นจำนวนเต็มตั้งแต่ 0 บาท")]
    public int Price { get; set; }

    [Display(Name = "URL รูปโรงแรม")]
    [StringLength(1000, ErrorMessage = "URL รูปยาวได้ไม่เกิน 1,000 ตัวอักษร")]
    public string? PhotoUrl { get; set; }

    public IFormFile? CoverUpload { get; set; }
    public HotelEditorGallery Gallery { get; set; } = new();
    public HotelEditorEnvironment? Environment { get; set; }
    public List<HotelEditorRoom> Rooms { get; set; } = [];
    public List<int> RemovePhotoIds { get; set; } = [];
    [BindNever] public IReadOnlyList<HotelGalleryPhoto> SavedPhotos { get; set; } = [];

    [Display(Name = "คำอธิบายที่พัก")]
    [StringLength(2500, ErrorMessage = "คำอธิบายยาวได้ไม่เกิน 2,500 ตัวอักษร")]
    public string? Description { get; set; }

    public List<string> Amenities { get; set; } = [];

    [Display(Name = "เปิดแสดงบนหน้าเว็บและรับจอง")]
    public bool IsActive { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Price == 0 && Rooms.Count == 0)
            yield return new ValidationResult("กรอกราคาอ้างอิงตั้งแต่ 1 บาท หรือเพิ่มประเภทห้องพร้อมราคา", [nameof(Price)]);
        if (!HotelCatalogRules.IsPhotoUrlValid(PhotoUrl))
            yield return new ValidationResult("ใช้ URL รูปที่ขึ้นต้นด้วย https:// หรือ http://", [nameof(PhotoUrl)]);
        if (CoverUpload is { Length: <= 0 or > UploadedFiles.MaxBytes })
            yield return new ValidationResult("รองรับไฟล์ JPG หรือ PNG ขนาดไม่เกิน 5 MB", [nameof(CoverUpload)]);
        if (Amenities.Any(amenity => !HotelCatalogRules.AmenityOptions.Contains(amenity)))
            yield return new ValidationResult("เลือกสิ่งอำนวยความสะดวกจากรายการ", [nameof(Amenities)]);
        if (Rooms.Count(room => string.IsNullOrWhiteSpace(room.Id)) > 30)
            yield return new ValidationResult("เพิ่มประเภทห้องใหม่ได้ไม่เกิน 30 ประเภทต่อครั้ง", [nameof(Rooms)]);
        var ids = Rooms.Where(room => !string.IsNullOrWhiteSpace(room.Id)).Select(room => room.Id).ToList();
        if (ids.Count != ids.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            yield return new ValidationResult("ประเภทห้องซ้ำกัน", [nameof(Rooms)]);
    }
}

public sealed class HotelEditorRoom : HotelRoomForm
{
    [StringLength(32)] public string? Id { get; set; }
    public HotelEditorGallery Gallery { get; set; } = new();
}

public sealed class HotelEditorGallery : IValidatableObject
{
    [Display(Name = "URL รูปภาพ แยกบรรทัดละหนึ่ง URL"), StringLength(10000)]
    public string? Urls { get; set; }

    [Display(Name = "อัปโหลดรูปภาพ")]
    public List<IFormFile> Uploads { get; set; } = [];

    public IEnumerable<HotelPhotoForm> Photos() =>
        (Urls ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(url => new HotelPhotoForm { Url = url })
            .Concat(Uploads.Select(upload => new HotelPhotoForm { Upload = upload }));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var entries = Photos().ToList();
        if (entries.Count > 20)
            yield return new ValidationResult("เพิ่มได้ไม่เกิน 20 รูปต่อแกลเลอรีในแต่ละครั้ง", [nameof(Urls), nameof(Uploads)]);
        if (entries.Any(entry => entry.Url is { } url &&
            (url.Length > 1000 || !HotelCatalogRules.IsPhotoUrlValid(url) || HotelPhotoService.IsUploadedUrl(url))))
            yield return new ValidationResult("กรอก URL รูปจาก http:// หรือ https:// ความยาวไม่เกิน 1,000 ตัวอักษร", [nameof(Urls)]);
    }
}

public sealed class HotelEditorEnvironment : IValidatableObject
{
    public string? Category { get; set; }
    public string? ViewType { get; set; }
    public bool? HasPool { get; set; }
    [Range(typeof(decimal), "0", "2000")] public decimal? SeaDistanceKm { get; set; }
    [Range(typeof(decimal), "0", "2000")] public decimal? ForestDistanceKm { get; set; }
    [StringLength(600)] public string? ViewDescription { get; set; }
    [StringLength(1000)] public string? SourceUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Category is { Length: > 0 } && !StayEnvironmentOptions.Categories.ContainsKey(Category))
            yield return new ValidationResult("เลือกหมวดบรรยากาศจากรายการ", [nameof(Category)]);
        if (ViewType is { Length: > 0 } && !StayEnvironmentOptions.Views.ContainsKey(ViewType))
            yield return new ValidationResult("เลือกวิวจากรายการ", [nameof(ViewType)]);
        if (!string.IsNullOrWhiteSpace(SourceUrl) &&
            (!Uri.TryCreate(SourceUrl.Trim(), UriKind.Absolute, out var link) ||
             link.Scheme is not ("http" or "https") || link.Host.Length == 0 || link.UserInfo.Length != 0))
            yield return new ValidationResult("กรอกลิงก์อ้างอิงจาก http:// หรือ https://", [nameof(SourceUrl)]);
    }
}

public sealed record HotelCatalogRow(string Id, string Name, string City, int Price, bool IsActive);
