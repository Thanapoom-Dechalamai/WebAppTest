using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
namespace Nontiwat_7362_L002.ViewComponents;

// Bell icon in the navbar with the number of unread notifications.
public class NotificationBellViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!int.TryParse(UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Content(string.Empty);
        var unread = await db.Notifications.AsNoTracking().CountAsync(x => x.UserId == userId && !x.IsRead);
        return View(unread);
    }
}
