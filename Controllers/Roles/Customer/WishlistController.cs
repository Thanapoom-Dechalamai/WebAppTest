using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Policy = RolePolicies.Customer)]
public class WishlistController(WishlistService wishlist) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The site's default action is Login, so /Wishlist needs its own route.
    [HttpGet("/Wishlist"), HttpGet("/Wishlist/Index")]
    public async Task<IActionResult> Index() => View(await wishlist.HotelsAsync(UserId));

    // Works as a plain form post (no JavaScript); wishlist.js sends the same form with fetch.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string hotelId, string? returnUrl)
    {
        bool liked;
        try { liked = await wishlist.ToggleAsync(UserId, hotelId ?? ""); }
        catch (KeyNotFoundException) { return NotFound(); }
        if (Request.Headers["X-Requested-With"] == "fetch") return Json(new { liked });
        TempData["Success"] = liked ? "เพิ่มในรายการโปรดแล้ว" : "เอาออกจากรายการโปรดแล้ว";
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
    }
}
