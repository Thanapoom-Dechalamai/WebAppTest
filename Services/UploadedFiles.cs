namespace Nontiwat_7362_L002.Services;

// Private uploads (transfer slips, proof of employment). Stored under App_Data, never in wwwroot,
// and served only through actions that check who is asking. The type is read from the file's first bytes.
public static class UploadedFiles
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const string Rule = "รองรับ JPG, PNG หรือ PDF ไม่เกิน 5 MB";

    public static string Folder(IWebHostEnvironment env, string kind) => Path.Combine(env.ContentRootPath, "App_Data", kind);

    // Returns the saved file name, or an error message.
    public static async Task<(string? Name, string? Error)> SaveAsync(IFormFile file, IWebHostEnvironment env, string kind, bool imagesOnly = false)
    {
        var rule = imagesOnly ? "รองรับไฟล์ JPG หรือ PNG ขนาดไม่เกิน 5 MB" : Rule;
        if (file.Length == 0 || file.Length > MaxBytes) return (null, rule);
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();
        var extension = ExtensionOf(bytes);
        if (extension is null || (imagesOnly && extension == ".pdf")) return (null, rule);
        var folder = Folder(env, kind);
        Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(folder, name), bytes);
        return (name, null);
    }

    public static string? ExtensionOf(byte[] b)
    {
        if (b.Length >= 24 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ".png";
        if (b.Length >= 4 && b[0] == 255 && b[1] == 216 && b[2] == 255) return ".jpg";
        if (b.Length >= 5 && b.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) return ".pdf";
        return null;
    }

    public static string ContentType(string name) => Path.GetExtension(name) switch
    {
        ".png" => "image/png", ".jpg" => "image/jpeg", ".pdf" => "application/pdf", _ => "application/octet-stream"
    };

    // A stored name is always <32 hex>.<ext>; anything else is refused before touching the disk.
    public static string? PathOf(IWebHostEnvironment env, string kind, string? name)
    {
        if (name is null || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[0-9a-f]{32}\\.(png|jpg|pdf)$")) return null;
        var path = Path.Combine(Folder(env, kind), name);
        return File.Exists(path) ? path : null;
    }
}
