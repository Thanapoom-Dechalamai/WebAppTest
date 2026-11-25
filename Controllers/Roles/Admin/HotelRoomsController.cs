using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class HotelRoomsController(HotelCatalogService hotels, HotelRoomCatalog rooms, HotelRoomAdminService editor) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string id)
    {
        var hotel = await hotels.FindAsync(id);
        return hotel is null ? NotFound() : View(new HotelRoomList(hotel, await rooms.ListAsync(hotel.Id, includeInactive: true)));
    }

    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] string id, [FromQuery] string? roomId)
    {
        var hotel = await hotels.FindAsync(id);
        if (hotel is null) return NotFound();
        ViewData["HotelName"] = hotel.Name;
        if (roomId is null) return View(new HotelRoomForm { Capacity = 1 });
        var room = await rooms.FindAsync(hotel.Id, roomId);
        if (room is null) return NotFound();
        return View(new HotelRoomForm
        {
            Name = room.Name, Price = room.NightlyRate(hotel.Price), Capacity = room.Capacity,
            SizeSquareMeters = room.SizeSquareMeters, BedType = room.BedType, BedCount = room.BedCount, Features = room.Features, IsActive = room.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit([FromRoute] string id, [FromQuery] string? roomId, HotelRoomForm form)
    {
        var hotel = await hotels.FindAsync(id);
        if (hotel is null) return NotFound();
        ViewData["HotelName"] = hotel.Name;
        if (!ModelState.IsValid) return View(form);
        try
        {
            if (!await editor.SaveAsync(hotel.Id, roomId, form)) return NotFound();
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(form.Capacity), error.Message);
            return View(form);
        }
        TempData["Success"] = "บันทึกข้อมูลห้องพักแล้ว";
        return RedirectToAction(nameof(Index), new { id = hotel.Id });
    }
}
