namespace Nontiwat_7362_L002.ViewModels.Roles;

// One measure per chart (never two y-scales). Format: "baht" or "count".
public record LineChart(string Id, string Title, IReadOnlyList<string> Labels, IReadOnlyList<double> Values, string Format)
{
    public string FormatValue(double v) => Format == "baht" ? "฿" + v.ToString("N0") : v.ToString("N0");
    public string FormatAxis(double v) => Format == "baht"
        ? (Math.Abs(v) >= 1_000_000 ? "฿" + (v / 1_000_000).ToString("0.#") + "M" : Math.Abs(v) >= 1_000 ? "฿" + (v / 1_000).ToString("0.#") + "K" : "฿" + v.ToString("0"))
        : v.ToString("0");
}

public record RankRow(string Key, string Name, decimal Amount, int Bookings);
public record OwnerWarning(string Kind, string Text, string Url);
