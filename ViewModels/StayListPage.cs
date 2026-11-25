namespace Nontiwat_7362_L002.ViewModels;

// The "ที่พัก" page (/Stays, 2026-11-22): every hotel as a list, filtered and sorted on the server from the query string.
public record StayListQuery(string? Destination, string? Q, string? Category, int? MinPrice, int? MaxPrice, double? Rating,
    string Sort, int Page, string? CheckIn, string? CheckOut, int? Guests, int? Rooms, string? Promo = null)
{
    public static readonly IReadOnlyDictionary<string, string> Sorts = new Dictionary<string, string>
    {
        ["recommended"] = "แนะนำ", ["price-asc"] = "ราคาต่ำไปสูง", ["price-desc"] = "ราคาสูงไปต่ำ", ["rating"] = "คะแนนรีวิวสูงสุด"
    };
    public static readonly double[] Ratings = [4.5, 4.0, 3.5];

    // A promotion code picked on the promotions page rides along to the hotel page, which hands it to checkout.
    public string Detail(string hotelId) => "/Stays/Details/" + Uri.EscapeDataString(hotelId) + (Promo is null ? "" : "?promo=" + Uri.EscapeDataString(Promo));

    // Query string for links that keep everything except the values given (null removes a key).
    public string With(IDictionary<string, string?> changes)
    {
        var values = new Dictionary<string, string?>
        {
            ["destination"] = Destination, ["q"] = Q, ["category"] = Category,
            ["minPrice"] = MinPrice?.ToString(), ["maxPrice"] = MaxPrice?.ToString(),
            ["rating"] = Rating?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["sort"] = Sort == "recommended" ? null : Sort, ["page"] = Page > 1 ? Page.ToString() : null,
            ["checkin"] = CheckIn, ["checkout"] = CheckOut, ["guests"] = Guests?.ToString(), ["rooms"] = Rooms?.ToString(), ["promo"] = Promo
        };
        foreach (var (k, v) in changes) values[k] = v;
        var parts = values.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value!)}");
        var qs = string.Join("&", parts);
        return "/Stays" + (qs.Length > 0 ? "?" + qs : "");
    }
}

public record StayListPage(StayListQuery Query, IReadOnlyList<HotelCardViewModel> Hotels, int Total, int PageCount,
    IReadOnlyList<string> Cities, int PriceCeiling, IReadOnlyList<HotelCardViewModel> All)
{
    public const int PageSize = 10;
}

public static class HotelMatching
{
    // Same rule the atmosphere bar has always used: the hotel's category, one of its themes, or a pool / mountain view.
    public static bool InCategory(HotelCardViewModel h, string? key) => string.IsNullOrEmpty(key)
        || h.Environment.Category == key || h.Themes.Contains(key)
        || (key == "pool" && h.Environment.HasPool == true) || (key == "mountain" && h.Environment.ViewType == "mountain");
}
