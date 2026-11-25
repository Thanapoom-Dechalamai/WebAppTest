using System.ComponentModel.DataAnnotations;
namespace Nontiwat_7362_L002.Models;

public class StayEnvironment
{
    [Required, StringLength(32)] public string HotelId { get; set; } = "";
    [RegularExpression("^(|beach|forest|mountain|city|river|garden|lake|ricefield)$")]
    public string Category { get; set; } = "";
    [RegularExpression("^(|sea|forest|mountain|city|river|garden|pool|lake|ricefield|golf)$")]
    public string ViewType { get; set; } = "";
    public bool? HasPool { get; set; }
    [Range(typeof(decimal), "0", "2000", ErrorMessage = "ระยะทางต้องอยู่ระหว่าง 0–2,000 กม.")]
    public decimal? SeaDistanceKm { get; set; }
    [Range(typeof(decimal), "0", "2000", ErrorMessage = "ระยะทางต้องอยู่ระหว่าง 0–2,000 กม.")]
    public decimal? ForestDistanceKm { get; set; }
    [StringLength(600)] public string? ViewDescription { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public static class StayEnvironmentOptions
{
    public static IReadOnlyDictionary<string,string> Categories { get; } = new Dictionary<string,string>
    { ["beach"]="พักใกล้ทะเล", ["forest"]="พักใกล้ป่า", ["mountain"]="พักชมภูเขา", ["city"]="พักในเมือง", ["river"]="พักริมแม่น้ำ", ["garden"]="พักในสวน", ["lake"]="ริมทะเลสาบ", ["ricefield"]="วิวทุ่งนา" };
    public static IReadOnlyDictionary<string,string> DiscoveryCategories { get; } = Categories.Concat(new Dictionary<string,string> { ["pool"]="พักพร้อมสระ", ["airport"]="ใกล้สนามบิน", ["shopping"]="ใกล้แหล่งช้อปปิ้ง", ["golf"]="พักใกล้สนามกอล์ฟ" }).ToDictionary(x=>x.Key,x=>x.Value);
    public static IReadOnlyDictionary<string,string> Views { get; } = new Dictionary<string,string>
    { ["sea"]="วิวทะเล", ["forest"]="วิวป่า", ["mountain"]="วิวภูเขา", ["city"]="วิวเมือง", ["river"]="วิวแม่น้ำ", ["garden"]="วิวสวน", ["pool"]="วิวสระว่ายน้ำ", ["lake"]="วิวทะเลสาบ", ["ricefield"]="วิวทุ่งนา", ["golf"]="วิวสนามกอล์ฟ" };
    public static string Distance(decimal? km) => km is null ? "ยังไม่ระบุ" : km == 0 ? "ติดพื้นที่ (0 ม.)" : km < 1 ? $"{km * 1000:0.##} ม." : $"{km:0.###} กม.";
}
