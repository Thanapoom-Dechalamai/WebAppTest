using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public sealed class HotelCatalogService(ApplicationDbContext db, HotelRoomCatalog rooms, HotelRoomAdminService editor,
    HotelPhotoService photos, IThaiClock clock)
{
    public Task<List<HotelCatalogRow>> ListAsync() => db.Hotels.AsNoTracking()
        .OrderByDescending(hotel => hotel.IsActive).ThenBy(hotel => hotel.Name).ThenBy(hotel => hotel.Id)
        .Select(hotel => new HotelCatalogRow(hotel.Id, hotel.Name, hotel.City, hotel.Price, hotel.IsActive))
        .ToListAsync();

    public Task<HotelData?> FindAsync(string id) =>
        db.Hotels.AsNoTracking().SingleOrDefaultAsync(hotel => hotel.Id == id);

    public async Task<int> MaxBasePriceAsync() =>
        HotelCatalogRules.MaxBasePrice(await db.RoomTypes.MaxAsync(room => (decimal?)room.Extra) ?? 0m);

    public async Task<HotelCatalogForm?> EditorAsync(string id)
    {
        var hotel = await FindAsync(id);
        if (hotel is null) return null;
        var environment = await db.StayEnvironments.AsNoTracking().SingleOrDefaultAsync(item => item.HotelId == id);
        return new HotelCatalogForm
        {
            Name = hotel.Name, City = hotel.City, Price = hotel.Price, PhotoUrl = hotel.PhotoUrl, IsActive = hotel.IsActive,
            Description = hotel.Description, Amenities = hotel.Amenities?.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [],
            SavedPhotos = await photos.ListAsync(id),
            Rooms = (await rooms.ListAsync(id, includeInactive: true)).Select(room => new HotelEditorRoom
            {
                Id = room.Id, Name = room.Name, Price = room.NightlyRate(hotel.Price), Capacity = room.Capacity, BedCount = room.BedCount,
                SizeSquareMeters = room.SizeSquareMeters, BedType = room.BedType, Features = room.Features, IsActive = room.IsActive
            }).ToList(),
            Environment = environment is null ? new() : new()
            {
                Category = environment.Category, ViewType = environment.ViewType, HasPool = environment.HasPool,
                SeaDistanceKm = environment.SeaDistanceKm, ForestDistanceKm = environment.ForestDistanceKm,
                ViewDescription = environment.ViewDescription, SourceUrl = environment.SourceUrl
            }
        };
    }

    public async Task RestoreEditorContextAsync(HotelData hotel, HotelCatalogForm form)
    {
        form.IsActive = hotel.IsActive;
        form.SavedPhotos = await photos.ListAsync(hotel.Id);
    }

    public async Task CreateAsync(HotelCatalogForm form)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var hotel = HotelCatalogRules.NewHotel(form);
        hotel.UseSharedRooms = form.Rooms.Count == 0;
        db.Hotels.Add(hotel);
        await SaveEditorAsync(hotel, form, transaction, creating: true);
    }

    public async Task<bool> EditAsync(string id, HotelCatalogForm form)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var hotel = (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {id} FOR UPDATE")
            .ToListAsync()).SingleOrDefault();
        if (hotel is null) return false;
        await SaveEditorAsync(hotel, form, transaction, creating: false);
        return true;
    }

    private async Task SaveEditorAsync(HotelData hotel, HotelCatalogForm form,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, bool creating)
    {
        var id = hotel.Id;
        if (form.PhotoUrl?.Trim() is { } url && HotelPhotoService.IsUploadedUrl(url) &&
            !await db.HotelGalleryPhotos.AnyAsync(photo => photo.HotelId == id && photo.RoomId == null && photo.Url == url))
            throw new ArgumentException("เลือกรูปที่อัปโหลดไว้ในแกลเลอรีของโรงแรมนี้");
        var addedFiles = new List<string>();
        var removedFiles = new List<string>();
        try
        {
            foreach (var room in form.Rooms)
            {
                if (creating && room.Id is not null) throw new ArgumentException("ประเภทห้องนี้ไม่ได้อยู่ในโรงแรมใหม่");
                var roomId = await editor.StageAsync(hotel, room.Id, room, preserveShared: true)
                    ?? throw new ArgumentException("ไม่พบประเภทห้องในโรงแรมนี้");
                await StageGalleryAsync(id, roomId, room.Gallery, addedFiles);
            }

            HotelCatalogRules.ApplyDetails(hotel, form);
            await StagePhotoRemovalsAsync(hotel, form.RemovePhotoIds, removedFiles);
            await StageGalleryAsync(id, null, form.Gallery, addedFiles);
            if (form.CoverUpload is not null)
            {
                hotel.PhotoUrl = await photos.StageAsync(id, null, new() { Upload = form.CoverUpload }, addedFiles);
                hotel.PhotoSourceUrl = null;
                hotel.PhotoCredit = null;
            }
            if (form.Environment is not null) await StageEnvironmentAsync(id, form.Environment);
            await db.SaveChangesAsync();
        }
        catch
        {
            photos.DeleteUploads(addedFiles);
            throw;
        }
        await transaction.CommitAsync();
        photos.DeleteUploads(removedFiles);
    }

    private async Task StageGalleryAsync(string hotelId, string? roomId, HotelEditorGallery gallery, List<string> files)
    {
        foreach (var photo in gallery.Photos())
            await photos.StageAsync(hotelId, roomId, photo, files);
    }

    private async Task StagePhotoRemovalsAsync(HotelData hotel, List<int> ids, List<string> files)
    {
        if (ids.Count == 0) return;
        var selected = ids.Distinct().ToList();
        var saved = await db.HotelGalleryPhotos.Where(photo => photo.HotelId == hotel.Id && selected.Contains(photo.Id)).ToListAsync();
        if (saved.Count != selected.Count) throw new ArgumentException("ไม่พบรูปภาพที่เลือกในโรงแรมนี้");
        foreach (var photo in saved)
        {
            if (photo.RoomId is null && photo.Url == hotel.PhotoUrl)
            {
                hotel.PhotoUrl = "";
                hotel.PhotoSourceUrl = null;
                hotel.PhotoCredit = null;
            }
            if (HotelPhotoService.IsUploadedUrl(photo.Url)) files.Add(Path.GetFileName(photo.Url));
        }
        db.HotelGalleryPhotos.RemoveRange(saved);
    }

    private async Task StageEnvironmentAsync(string hotelId, HotelEditorEnvironment form)
    {
        var environment = await db.StayEnvironments.FindAsync(hotelId);
        if (environment is null)
        {
            environment = new StayEnvironment { HotelId = hotelId };
            db.StayEnvironments.Add(environment);
        }
        var category = form.Category ?? "";
        var view = form.ViewType ?? "";
        var description = form.ViewDescription?.Trim();
        var source = form.SourceUrl?.Trim();
        if (environment.Category == category && environment.ViewType == view && environment.HasPool == form.HasPool &&
            environment.SeaDistanceKm == form.SeaDistanceKm && environment.ForestDistanceKm == form.ForestDistanceKm &&
            environment.ViewDescription == description && environment.SourceUrl == source && environment.UpdatedAtUtc != default)
            return;
        environment.Category = category;
        environment.ViewType = view;
        environment.HasPool = form.HasPool;
        environment.SeaDistanceKm = form.SeaDistanceKm;
        environment.ForestDistanceKm = form.ForestDistanceKm;
        environment.ViewDescription = description;
        environment.SourceUrl = source;
        environment.VerifiedAtUtc = null;
        environment.UpdatedAtUtc = clock.UtcNow;
    }

    public async Task<bool> SetActiveAsync(string id, bool active)
    {
        var hotel = await db.Hotels.SingleOrDefaultAsync(hotel => hotel.Id == id);
        if (hotel is null) return false;
        // Booking takes a row lock on this hotel and rechecks this flag before creating an order.
        hotel.IsActive = active;
        await db.SaveChangesAsync();
        return true;
    }
}
