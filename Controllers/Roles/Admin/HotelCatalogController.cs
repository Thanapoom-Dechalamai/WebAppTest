using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
public sealed class HotelCatalogController(HotelCatalogService catalog) : Controller
{
    private const long EditorRequestBytes = 32 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Index() => View(await catalog.ListAsync());

    [HttpGet]
    public IActionResult Create() => View("Editor", new HotelCatalogForm { IsActive = true, Environment = new() });

    [HttpPost, RequestSizeLimit(EditorRequestBytes), RequestFormLimits(ValueCountLimit = 8192)]
    public async Task<IActionResult> Create(HotelCatalogForm form)
    {
        await ValidateFormAsync(null, form);
        if (!ModelState.IsValid) return View("Editor", form);
        try { await catalog.CreateAsync(form); }
        catch (ArgumentException error)
        {
            ModelState.AddModelError("", error.Message);
            return View("Editor", form);
        }
        TempData["Success"] = "เพิ่มโรงแรมแล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] string id)
    {
        var form = await catalog.EditorAsync(id);
        if (form is null) return NotFound();
        ViewData["OriginalCity"] = form.City;
        ViewData["SavedCover"] = form.PhotoUrl;
        return View("Editor", form);
    }

    [HttpPost, RequestSizeLimit(EditorRequestBytes), RequestFormLimits(ValueCountLimit = 8192)]
    public async Task<IActionResult> Edit([FromRoute] string id, HotelCatalogForm form)
    {
        var hotel = await catalog.FindAsync(id);
        if (hotel is null) return NotFound();
        ViewData["OriginalCity"] = hotel.City;
        ViewData["SavedCover"] = hotel.PhotoUrl;
        await ValidateFormAsync(hotel, form);
        if (!ModelState.IsValid)
        {
            await catalog.RestoreEditorContextAsync(hotel, form);
            return View("Editor", form);
        }
        try
        {
            if (!await catalog.EditAsync(id, form)) return NotFound();
        }
        catch (ArgumentException error)
        {
            await catalog.RestoreEditorContextAsync(hotel, form);
            ModelState.AddModelError("", error.Message);
            return View("Editor", form);
        }
        TempData["Success"] = "บันทึกข้อมูลโรงแรมแล้ว";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(HotelData? hotel, HotelCatalogForm form)
    {
        if (!ModelState.IsValid) return;
        var city = form.City.Trim();
        if (!ThaiProvinces.All.Contains(city) && hotel?.City != city)
            ModelState.AddModelError(nameof(form.City), "เลือกจังหวัดจากรายการ");
        var limit = await catalog.MaxBasePriceAsync();
        if (form.Price == 0 && form.Rooms.Count > 0)
            form.Price = hotel?.Price ?? Math.Min(limit, (int)decimal.Floor(form.Rooms.Min(room => room.Price)));
        if (form.Price > limit)
            ModelState.AddModelError(nameof(form.Price), $"ราคาต่อคืนต้องไม่เกิน {limit:N0} บาท");
        if (hotel is null && form.PhotoUrl?.Trim() is { } url && HotelPhotoService.IsUploadedUrl(url))
            ModelState.AddModelError(nameof(form.PhotoUrl), "เลือกรูปที่อัปโหลดไว้ในแกลเลอรีของโรงแรมนี้");
    }

    [HttpGet]
    public async Task<IActionResult> Archive([FromRoute] string id)
    {
        var hotel = await catalog.FindAsync(id);
        if (hotel is null) return NotFound();
        if (!hotel.IsActive) return RedirectToAction(nameof(Index));
        return View(new HotelCatalogRow(hotel.Id, hotel.Name, hotel.City, hotel.Price, hotel.IsActive));
    }

    [HttpPost, ActionName(nameof(Archive))]
    public async Task<IActionResult> ConfirmArchive([FromRoute] string id)
    {
        if (!await catalog.SetActiveAsync(id, false)) return NotFound();
        TempData["Success"] = "นำโรงแรมออกจากหน้าเว็บและหยุดรับจองใหม่แล้ว";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Restore([FromRoute] string id)
    {
        if (!await catalog.SetActiveAsync(id, true)) return NotFound();
        TempData["Success"] = "แสดงโรงแรมบนหน้าเว็บและเปิดรับจองอีกครั้งแล้ว";
        return RedirectToAction(nameof(Index));
    }
}
