using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Controllers;

// Every signed-in account has its own notifications (customers and hotel staff receive them today).
[Authorize]
public class NotificationsController(ApplicationDbContext db) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index(bool unread = false)
    {
        var mine = db.Notifications.AsNoTracking().Where(x => x.UserId == UserId);
        var count = await mine.CountAsync(x => !x.IsRead);
        var items = await (unread ? mine.Where(x => !x.IsRead) : mine).OrderByDescending(x => x.Id).Take(100).ToListAsync();
        return View(new NotificationListPage(items, count, unread));
    }

    // Opens what the notification is about and marks it read.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(long id)
    {
        var item = await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (item is null) return RedirectToAction(nameof(Index));
        if (!item.IsRead) { item.IsRead = true; await db.SaveChangesAsync(); }
        return Url.IsLocalUrl(item.Url) ? LocalRedirect(item.Url) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll()
    {
        await db.Notifications.Where(x => x.UserId == UserId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true));
        TempData["Success"] = "อ่านการแจ้งเตือนทั้งหมดแล้ว";
        return RedirectToAction(nameof(Index));
    }
}
