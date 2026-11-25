using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Controllers;
[Authorize(Roles=AppRoles.Admin)]
public class HotelManagementController(ApplicationDbContext db, HotelOwnershipService ownership, Nontiwat_7362_L002.Services.CatalogService catalog) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(new HotelManagementPage(await catalog.AllAsync(),
        await db.Users.AsNoTracking().Where(x => x.Role == AppRoles.HotelOwner).OrderBy(x => x.FirstName)
            .Select(x => new UserRowViewModel(x.Id,x.Email,x.FirstName,x.LastName,x.Role,x.CreatedAtUtc)).ToListAsync(),
        await db.HotelAssignments.AsNoTracking().ToListAsync())
        { Rates = await db.Hotels.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.CommissionPercent) });
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignHotelForm form)
    {
        if(!ModelState.IsValid)return BadRequest("เลือกโรงแรมและพนักงานให้ครบ");
        try {
            await ownership.AssignAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),form.HotelId,form.OwnerUserId);
            TempData["Success"]="เพิ่มพนักงานเข้าทีมโรงแรมแล้ว พนักงานคนนี้จะเห็นเฉพาะการจองของโรงแรมในทีมของตน";
        } catch(ArgumentException e){TempData["Error"]=e.Message;}
        catch(UnauthorizedAccessException){return Forbid();}
        return RedirectToAction(nameof(Index));
    }
    // Lodgo's share for new bookings at this hotel. Bookings already made keep the rate they were made with.
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(string hotelId, decimal percent)
    {
        if (percent < 0 || percent > Nontiwat_7362_L002.Models.FinanceRules.MaxCommissionPercent || decimal.Round(percent, 2) != percent)
        { TempData["Error"] = $"ค่าธรรมเนียมต้องอยู่ระหว่าง 0–{Nontiwat_7362_L002.Models.FinanceRules.MaxCommissionPercent:0}%"; return RedirectToAction(nameof(Index)); }
        var hotel = await db.Hotels.SingleOrDefaultAsync(x => x.Id == hotelId);
        if (hotel is null) return NotFound();
        hotel.CommissionPercent = percent;
        await db.SaveChangesAsync();
        TempData["Success"] = $"{hotel.Name}: ค่าธรรมเนียม Lodgo {percent:0.##}% โรงแรมได้รับ {100 - percent:0.##}% มีผลกับการจองใหม่เท่านั้น";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(AssignHotelForm form)
    {
        if(!ModelState.IsValid)return BadRequest("เลือกโรงแรมและพนักงานให้ครบ");
        try {
            await ownership.RemoveAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),form.HotelId,form.OwnerUserId);
            TempData["Success"]="นำพนักงานออกจากทีมโรงแรมแล้ว ประวัติยอดรับของโรงแรมยังอยู่ครบ";
        } catch(UnauthorizedAccessException){return Forbid();}
        return RedirectToAction(nameof(Index));
    }
}
