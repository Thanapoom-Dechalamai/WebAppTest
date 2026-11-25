using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels.Roles;

namespace Nontiwat_7362_L002.Controllers;
[Authorize(Policy = RolePolicies.Customer)]
public class CustomerController(ApplicationDbContext db, Nontiwat_7362_L002.Services.IThaiClock clock) : Controller
{
    public async Task<IActionResult> Index()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(new CustomerDashboard(await db.Orders.CountAsync(x => x.UserId == id),
            await db.Wallets.Where(x => x.UserId == id).Select(x => x.Balance).SingleOrDefaultAsync()));
    }
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Promotions()
    {
        var today = clock.Today;
        if(int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var userId))
          ViewData["UsedPromotions"]=await db.PromotionRedemptions.Where(x=>x.UserId==userId).Select(x=>x.PromotionId).ToListAsync();
        return View(await db.Promotions.AsNoTracking().Where(x => x.IsActive && x.StartsOn <= today && x.EndsOn >= today)
            .OrderByDescending(x => x.UpdatedAtUtc).ToListAsync());
    }
}
