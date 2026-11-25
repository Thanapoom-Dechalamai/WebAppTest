using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class HotelPhotosController(HotelCatalogService catalog, HotelRoomCatalog rooms, HotelPhotoService photos) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string id, [FromQuery] string? roomId)
    {
        var page = await PageAsync(id, roomId, new());
        return page is null ? NotFound() : View(page);
    }

    [HttpPost, RequestSizeLimit(UploadedFiles.MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Add([FromRoute] string id, [FromQuery] string? roomId, [Bind(Prefix = "Form")] HotelPhotoForm form)
    {
        var page = await PageAsync(id, roomId, form);
        if (page is null) return NotFound();
        if (!ModelState.IsValid) return View("Index", page);
        var error = await photos.AddAsync(page.Hotel.Id, page.Room?.Id, form);
        if (error is not null)
        {
            ModelState.AddModelError("Form.Upload", error);
            return View("Index", page);
        }
        TempData["Success"] = "เพิ่มรูปภาพแล้ว";
        return RedirectToAction(nameof(Index), new { id = page.Hotel.Id, roomId = page.Room?.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Main([FromRoute] string id, int photoId)
    {
        if (!await photos.SetMainAsync(id, photoId)) return NotFound();
        TempData["Success"] = "ตั้งรูปหลักแล้ว";
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromRoute] string id, [FromQuery] string? roomId, int photoId)
    {
        if (!await photos.RemoveAsync(id, roomId, photoId)) return NotFound();
        TempData["Success"] = "นำรูปออกแล้ว";
        return RedirectToAction(nameof(Index), new { id, roomId });
    }

    [AllowAnonymous, HttpGet("/HotelPhotos/Image/{name}")]
    public async Task<IActionResult> Image(string name)
    {
        var path = await photos.ReadPathAsync(name, User.IsInRole(AppRoles.Admin));
        if (path is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, UploadedFiles.ContentType(name));
    }

    private async Task<HotelPhotoPage?> PageAsync(string hotelId, string? roomId, HotelPhotoForm form)
    {
        var hotel = await catalog.FindAsync(hotelId);
        if (hotel is null) return null;
        var room = roomId is null ? null : await rooms.FindAsync(hotel.Id, roomId);
        if (roomId is not null && room is null) return null;
        var gallery = (await photos.ListAsync(hotel.Id)).Where(photo => photo.RoomId == room?.Id).ToList();
        return new(hotel, room, gallery, form);
    }
}
