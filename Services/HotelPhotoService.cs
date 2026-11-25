using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public sealed class HotelPhotoService(ApplicationDbContext db, IWebHostEnvironment environment, HotelRoomCatalog rooms, ILogger<HotelPhotoService> logger)
{
    private const string Folder = "HotelPhotos";
    private const string Route = "/HotelPhotos/Image/";

    public static bool IsUploadedUrl(string url) =>
        url.StartsWith(Route, StringComparison.Ordinal) && IsImageName(url[Route.Length..]);

    private static bool IsImageName(string name) => Regex.IsMatch(name, @"\A[0-9a-f]{32}\.(jpg|png)\z");

    public async Task<IReadOnlyList<HotelGalleryPhoto>> ListAsync(string hotelId) =>
        await db.HotelGalleryPhotos.AsNoTracking().Where(photo => photo.HotelId == hotelId).OrderBy(photo => photo.Id).ToListAsync();

    public async Task<string?> AddAsync(string hotelId, string? roomId, HotelPhotoForm form)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (await LockHotelAsync(hotelId) is null) return "ไม่พบโรงแรม";
        var files = new List<string>();
        try
        {
            await StageAsync(hotelId, roomId, form, files);
            await db.SaveChangesAsync();
        }
        catch (ArgumentException error) { DeleteUploads(files); return error.Message; }
        catch
        {
            DeleteUploads(files);
            throw;
        }
        // If the connection fails during commit, retain the file because the database may have committed.
        await transaction.CommitAsync();
        return null;
    }

    internal async Task<string> StageAsync(string hotelId, string? roomId, HotelPhotoForm form, List<string> files)
    {
        var url = form.Url?.Trim() ?? "";
        if (form.Upload is not null)
        {
            var (name, error) = await UploadedFiles.SaveAsync(form.Upload, environment, Folder, imagesOnly: true);
            if (name is null) throw new ArgumentException(error);
            files.Add(name);
            url = Route + name;
        }
        else if (db.HotelGalleryPhotos.Local.Any(photo => photo.HotelId == hotelId && photo.RoomId == roomId && photo.Url == url)
            || await db.HotelGalleryPhotos.AnyAsync(photo => photo.HotelId == hotelId && photo.RoomId == roomId && photo.Url == url))
            throw new ArgumentException("มีรูปจาก URL นี้อยู่แล้ว");
        db.HotelGalleryPhotos.Add(new HotelGalleryPhoto { HotelId = hotelId, RoomId = roomId, Url = url, Credit = form.Credit?.Trim() });
        return url;
    }

    internal void DeleteUploads(IEnumerable<string> files)
    {
        foreach (var file in files) DeleteFile(file);
    }

    public async Task<bool> SetMainAsync(string hotelId, int photoId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var hotel = await LockHotelAsync(hotelId);
        var photo = await db.HotelGalleryPhotos.SingleOrDefaultAsync(x => x.Id == photoId && x.HotelId == hotelId && x.RoomId == null);
        if (hotel is null || photo is null) return false;
        hotel.PhotoUrl = photo.Url;
        hotel.PhotoSourceUrl = null;
        hotel.PhotoCredit = photo.Credit;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(string hotelId, string? roomId, int photoId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var hotel = await LockHotelAsync(hotelId);
        var photo = await db.HotelGalleryPhotos.SingleOrDefaultAsync(x => x.Id == photoId && x.HotelId == hotelId && x.RoomId == roomId);
        if (hotel is null || photo is null) return false;
        if (photo.RoomId is null && hotel.PhotoUrl == photo.Url)
        {
            hotel.PhotoUrl = "";
            hotel.PhotoSourceUrl = null;
            hotel.PhotoCredit = null;
        }
        db.HotelGalleryPhotos.Remove(photo);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        if (IsUploadedUrl(photo.Url)) DeleteFile(photo.Url[Route.Length..]);
        return true;
    }

    public async Task<string?> ReadPathAsync(string name, bool admin)
    {
        if (!IsImageName(name)) return null;
        var url = Route + name;
        var owner = await (from photo in db.HotelGalleryPhotos.AsNoTracking()
                           join hotel in db.Hotels.AsNoTracking() on photo.HotelId equals hotel.Id
                           where photo.Url == url && (admin || hotel.IsActive)
                           select new { photo.HotelId, photo.RoomId }).FirstOrDefaultAsync();
        if (owner is null) return null;
        if (!admin && owner.RoomId is not null && await rooms.FindAsync(owner.HotelId, owner.RoomId) is not { IsActive: true })
            return null;
        return UploadedFiles.PathOf(environment, Folder, name);
    }

    private async Task<HotelData?> LockHotelAsync(string id) =>
        (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {id} FOR UPDATE").ToListAsync()).SingleOrDefault();

    private void DeleteFile(string? name)
    {
        var path = UploadedFiles.PathOf(environment, Folder, name);
        if (path is null) return;
        try { System.IO.File.Delete(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Could not delete hotel photo file {PhotoName}", name);
        }
    }
}
