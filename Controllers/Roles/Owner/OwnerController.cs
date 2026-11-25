using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.ViewModels;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Controllers;
[Authorize(Policy = RolePolicies.Owner)]
public class OwnerController(ApplicationDbContext db, Nontiwat_7362_L002.Services.MonthlyFigures figures, Nontiwat_7362_L002.Services.OwnerInsights insights, Nontiwat_7362_L002.Services.IThaiClock clock) : Controller
{
    public async Task<IActionResult> Index() => View(new OwnerDashboard(
        await db.Users.CountAsync(x => x.Role == AppRoles.Customer),
        await db.Users.CountAsync(x => x.Role != AppRoles.Customer),
        await db.Hotels.CountAsync(x=>x.IsActive), await db.Hotels.Where(x=>x.IsActive).Select(x=>x.City).Distinct().CountAsync(),
        await db.Orders.CountAsync(), await db.Orders.CountAsync(x => x.Status == "Cancelled"),
        -(await db.WalletEntries.Where(x => x.Kind == "Payment").SumAsync(x => (decimal?)x.Amount) ?? 0),
        await db.WalletEntries.Where(x => x.Kind == "Refund").SumAsync(x => (decimal?)x.Amount) ?? 0)
    {
        Months = await figures.RangeAsync(clock.ThisMonth.AddMonths(-11), clock.ThisMonth),
        TopHotels = await insights.TopHotelsAsync(5),
        TopProvinces = await insights.TopProvincesAsync(5),
        Warnings = await insights.WarningsAsync()
    });
}
