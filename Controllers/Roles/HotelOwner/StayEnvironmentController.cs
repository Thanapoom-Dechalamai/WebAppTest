using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Controllers;

[Authorize(Policy = RolePolicies.HotelOwner)]
public class StayEnvironmentController(ApplicationDbContext db, Nontiwat_7362_L002.Services.CatalogService catalog) : Controller
{
    private async Task<bool> CanEdit(string id) => User.IsInRole(AppRoles.Admin) ||
        await db.HotelAssignments.AnyAsync(x => x.HotelId == id &&
            x.OwnerUserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

    private async Task LoadHotels()
    {
        var actor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ids = await db.HotelAssignments.AsNoTracking().Where(x => x.OwnerUserId == actor).Select(x => x.HotelId).ToListAsync();
        var admin = User.IsInRole(AppRoles.Admin);
        ViewBag.Hotels = (await catalog.AllAsync(includeInactive: admin)).Where(x => admin || ids.Contains(x.Id)).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string? id)
    {
        await LoadHotels();
        if (string.IsNullOrEmpty(id)) return View(new StayEnvironment());
        if (!await db.Hotels.AnyAsync(x => x.Id == id && (x.IsActive || User.IsInRole(AppRoles.Admin)))) return NotFound();
        if (!await CanEdit(id)) return Forbid();
        return View(await db.StayEnvironments.AsNoTracking().SingleOrDefaultAsync(x => x.HotelId == id)
            ?? new StayEnvironment { HotelId = id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind("HotelId,Category,ViewType,HasPool,SeaDistanceKm,ForestDistanceKm,ViewDescription")] StayEnvironment form)
    {
        // Empty selects represent unknown values, not an inferred environment.
        form.Category ??= "";
        form.ViewType ??= "";
        ModelState.Remove(nameof(form.Category));
        ModelState.Remove(nameof(form.ViewType));
        if (form.Category != "" && !StayEnvironmentOptions.Categories.ContainsKey(form.Category))
            ModelState.AddModelError(nameof(form.Category), "หมวดหมู่ไม่ถูกต้อง");
        if (form.ViewType != "" && !StayEnvironmentOptions.Views.ContainsKey(form.ViewType))
            ModelState.AddModelError(nameof(form.ViewType), "ประเภทวิวไม่ถูกต้อง");
        if (!await db.Hotels.AnyAsync(x => x.Id == form.HotelId && (x.IsActive || User.IsInRole(AppRoles.Admin)))) return NotFound();
        if (!await CanEdit(form.HotelId)) return Forbid();
        if (!ModelState.IsValid) { await LoadHotels(); return View(form); }
        var entity = await db.StayEnvironments.SingleOrDefaultAsync(x => x.HotelId == form.HotelId);
        if (entity is null) { entity = new StayEnvironment { HotelId = form.HotelId }; db.StayEnvironments.Add(entity); }
        entity.Category = form.Category;
        entity.ViewType = form.ViewType;
        entity.HasPool = form.HasPool;
        entity.SeaDistanceKm = form.SeaDistanceKm;
        entity.ForestDistanceKm = form.ForestDistanceKm;
        entity.ViewDescription = form.ViewDescription?.Trim();
        entity.SourceUrl=null; entity.VerifiedAtUtc=null;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "บันทึกบรรยากาศและสิ่งอำนวยความสะดวกแล้ว";
        return RedirectToAction(nameof(Edit), null, new { id = form.HotelId }, "environment-editor");
    }
}
