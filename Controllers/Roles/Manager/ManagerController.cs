using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Controllers;

[Authorize(Policy = RolePolicies.Manager)]
public class ManagerController(ApplicationDbContext db, UserService users) : Controller
{
    private bool CanAssign(string role) => User.IsInRole(AppRoles.Admin) ? AppRoles.IsValid(role) : AppRoles.TeamRoles.Contains(role);
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        search = search?.Trim();
        if (search?.Length > 100) search = search[..100];
        var query = db.Users.AsNoTracking();
        if (!User.IsInRole(AppRoles.Admin)) query = query.Where(x => AppRoles.TeamRoles.Contains(x.Role));
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search) || x.Email.Contains(search));
        return View(new UserListViewModel { Search = search, Users = await query.OrderByDescending(x => x.Id).Take(200)
            .Select(x => new UserRowViewModel(x.Id, x.Email, x.FirstName, x.LastName, x.Role, x.CreatedAtUtc)).ToListAsync() });
    }
    [HttpGet]
    public async Task<IActionResult> Kpi(int? days, [FromServices] KpiService kpi) => View(await kpi.ForAsync(KpiService.NormalizeDays(days)));

    [HttpGet]
    public IActionResult AddEmployee() => View(new AddUserViewModel { Role = AppRoles.Staff });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEmployee(AddUserViewModel form)
    {
        if (!CanAssign(form.Role)) return Forbid();
        if (!ModelState.IsValid) return View(form);
        var result = await users.CreateAsync(form, form.Role);
        if (result.DuplicateEmail) { ModelState.AddModelError(nameof(form.Email), "อีเมลนี้มีผู้ใช้งานแล้ว"); return View(form); }
        TempData["Success"] = "เพิ่มบัญชีทีมงานแล้ว";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(ChangeRoleViewModel form, [FromServices] SupportTicketService tickets)
    {
        if (!ModelState.IsValid) return BadRequest("บทบาทไม่ถูกต้อง");
        if (!CanAssign(form.Role) || User.FindFirstValue(ClaimTypes.NameIdentifier) == form.Id.ToString()) return Forbid();
        var query = db.Users.Where(x => x.Id == form.Id);
        // Apply the restriction in the UPDATE itself, even if another admin changed this user concurrently.
        if (!User.IsInRole(AppRoles.Admin)) query = query.Where(x => AppRoles.TeamRoles.Contains(x.Role));
        var count = await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, form.Role));
        if (count == 0) return Forbid();
        var released = await tickets.ReleaseIfCannotOwnAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), form.Id);
        TempData["Success"] = "บันทึกบทบาทแล้ว" + (released > 0 ? $" · ปล่อย ticket ที่ดูแลอยู่ {released} เรื่องกลับไปที่ \"ยังไม่มีคนรับ\"" : "");
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> Promotions() => View(await db.Promotions.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync());
    [HttpGet]
    public async Task<IActionResult> EditPromotion(int? id)
    {
        if (id is null) return View(new PromotionForm());
        var p = await db.Promotions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : View(new PromotionForm { Id=p.Id, Code=p.Code, DiscountKind=p.DiscountKind, DiscountValue=p.DiscountValue, MinNights=p.MinNights, MinSpend=p.MinSpend, MaxDiscount=p.MaxDiscount, Title=p.Title, Details=p.Details, StartsOn=p.StartsOn, EndsOn=p.EndsOn, IsActive=p.IsActive });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPromotion(PromotionForm form)
    {
        form.Code=string.IsNullOrWhiteSpace(form.Code)?null:form.Code.Trim().ToUpperInvariant();
        if (!ModelState.IsValid) return View(form);
        if(form.Code is not null && await db.Promotions.AnyAsync(x=>x.Code==form.Code && x.Id!=form.Id)) {
            ModelState.AddModelError(nameof(form.Code),"โค้ดนี้มีอยู่แล้ว"); return View(form);
        }
        var promotion = form.Id == 0 ? new Promotion() : await db.Promotions.SingleOrDefaultAsync(x => x.Id == form.Id);
        if (promotion is null) return NotFound();
        if(promotion.Code!=form.Code && await db.PromotionRedemptions.AnyAsync(x=>x.PromotionId==promotion.Id)) {
            ModelState.AddModelError(nameof(form.Code),"โค้ดที่ถูกใช้แล้วเปลี่ยนชื่อไม่ได้ ให้ปิดแล้วสร้างโปรโมชั่นใหม่"); return View(form);
        }
        promotion.Code=form.Code; promotion.DiscountKind=form.DiscountKind; promotion.DiscountValue=form.DiscountValue;
        promotion.MinNights=form.MinNights; promotion.MinSpend=form.MinSpend; promotion.MaxDiscount=form.MaxDiscount;
        promotion.Title = form.Title.Trim(); promotion.Details = form.Details.Trim();
        promotion.StartsOn = form.StartsOn.Date; promotion.EndsOn = form.EndsOn.Date; promotion.IsActive = form.IsActive;
        promotion.UpdatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        promotion.UpdatedAtUtc = DateTime.UtcNow;
        if (form.Id == 0) db.Promotions.Add(promotion);
        try { await db.SaveChangesAsync(); }
        catch(DbUpdateException e) when(e.InnerException is MySqlConnector.MySqlException { Number:1062 }) {
            ModelState.AddModelError(nameof(form.Code),"โค้ดนี้มีอยู่แล้ว กรุณาใช้ชื่ออื่น"); return View(form);
        }
        TempData["Success"] = "บันทึกโปรโมชั่นและเงื่อนไขส่วนลดแล้ว";
        return RedirectToAction(nameof(Promotions));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchivePromotion(int id)
    {
        var p = await db.Promotions.SingleOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        p.IsActive = false; p.UpdatedAtUtc=DateTime.UtcNow;
        p.UpdatedByUserId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await db.SaveChangesAsync();
        TempData["Success"]="ปิดประกาศโปรโมชั่นแล้ว";
        return RedirectToAction(nameof(Promotions));
    }
}
