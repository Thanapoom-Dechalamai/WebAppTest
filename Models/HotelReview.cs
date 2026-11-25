namespace Nontiwat_7362_L002.Models;

// A guest review on a 1–5 star scale. Reviews written in the app come from a finished
// stay (OrderId is set). Sample reviews (IsSample) are generated test data and are
// always labelled as such on the page.
public class HotelReview
{
    public int Id { get; set; }
    public string HotelId { get; set; } = "";
    public int? OrderId { get; set; }
    public int? UserId { get; set; }
    public string ReviewerName { get; set; } = "";
    public string ReviewerCountry { get; set; } = "ไทย";
    public string TravelerType { get; set; } = "";
    public int Rating { get; set; }
    public int Cleanliness { get; set; }
    public int Location { get; set; }
    public int Service { get; set; }
    public int Value { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string RoomName { get; set; } = "";
    public DateTime StayedOn { get; set; }
    public bool IsSample { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    // The hotel's public answer, shown under the review as "ตอบกลับจากที่พัก".
    public string? ReplyBody { get; set; }
    public int? ReplyByUserId { get; set; }
    public DateTime? ReplyAtUtc { get; set; }
}

public static class ReviewOptions
{
    public static readonly IReadOnlyDictionary<string, string> TravelerTypes = new Dictionary<string, string>
    {
        ["couple"] = "คู่รัก", ["family"] = "ครอบครัว", ["solo"] = "เดินทางคนเดียว", ["friends"] = "กลุ่มเพื่อน", ["business"] = "ทริปธุรกิจ"
    };
    public static string TravelerLabel(string key) => TravelerTypes.TryGetValue(key, out var label) ? label : key;
    public static readonly (string Key, string Label)[] Aspects =
        [("Cleanliness", "ความสะอาด"), ("Location", "ทำเลที่ตั้ง"), ("Service", "การบริการ"), ("Value", "ความคุ้มค่า")];
    public static string ScoreLabel(double score) => score switch
    {
        >= 4.5 => "ยอดเยี่ยม", >= 4.0 => "ดีมาก", >= 3.5 => "ดี", >= 3.0 => "พอใช้", _ => "ควรปรับปรุง"
    };
}
