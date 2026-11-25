using System.Globalization;
using System.Text;

namespace Nontiwat_7362_L002.Services;

// CSV that Excel opens correctly in Thai: UTF-8 with BOM, CRLF, invariant numbers.
public static class CsvExport
{
    public static byte[] Build(IEnumerable<string> header, IEnumerable<IEnumerable<object?>> rows)
    {
        var sb = new StringBuilder();
        void Line(IEnumerable<object?> cells) => sb.Append(string.Join(",", cells.Select(Cell))).Append("\r\n");
        Line(header);
        foreach (var row in rows) Line(row);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Cell(object? value)
    {
        var text = value switch
        {
            null => "",
            decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
            double d => d.ToString("0.##", CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            DateTime t => t.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            // Text typed by people (names, notes) must not start a spreadsheet formula. Numbers are left alone.
            string s when s.Length > 0 && "=+-@\t\r".Contains(s[0]) => "'" + s,
            _ => value.ToString() ?? ""
        };
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }
}
