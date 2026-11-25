using System.Security.Claims;
using Nontiwat_7362_L002.Services.Booking;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize]
public class MemberController(ApplicationDbContext db, WalletService walletService, IWebHostEnvironment env, UserService users) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == UserId);
        var avatar = await db.Wallets.Where(x => x.UserId == UserId).Select(x => x.AvatarFile).SingleOrDefaultAsync();
        return View(new ProfileForm { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, Role = user.Role, AvatarFile = avatar });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Profile(ProfileForm form)
    {
        var user = await db.Users.SingleAsync(x => x.Id == UserId);
        await walletService.EnsureWalletAsync(UserId);
        var wallet = await db.Wallets.SingleAsync(x => x.UserId == UserId);
        // Email, role and avatar path are never trusted from submitted fields.
        form.Email = user.Email; form.Role = user.Role; form.AvatarFile = wallet.AvatarFile;
        if (string.IsNullOrWhiteSpace(form.FirstName) || string.IsNullOrWhiteSpace(form.LastName))
            ModelState.AddModelError("", "กรุณากรอกชื่อและนามสกุล");
        byte[]? image = null;
        string? extension = null;
        if (form.Photo is { Length: > 0 } photo)
        {
            if (photo.Length > 2 * 1024 * 1024) ModelState.AddModelError("Photo", "รูปต้องมีขนาดไม่เกิน 2 MB");
            else
            {
                using var stream = new MemoryStream();
                await photo.CopyToAsync(stream);
                image = stream.ToArray();
                if (image.Length >= 24 && image.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})) extension = ".png";
                else if (image.Length >= 4 && image[0] == 255 && image[1] == 216 && image[2] == 255 && image[^2] == 255 && image[^1] == 217) extension = ".jpg";
                else ModelState.AddModelError("Photo", "รองรับไฟล์ภาพ JPG และ PNG เท่านั้น");
            }
        }
        if (!ModelState.IsValid) return View(form);
        if (image is not null && extension is not null)
        {
            var folder = Path.Combine(env.ContentRootPath, "App_Data", "Avatars");
            Directory.CreateDirectory(folder);
            wallet.AvatarFile = Guid.NewGuid().ToString("N") + extension;
            await System.IO.File.WriteAllBytesAsync(Path.Combine(folder, wallet.AvatarFile), image);
        }
        user.FirstName = form.FirstName.Trim(); user.LastName = form.LastName.Trim();
        await db.SaveChangesAsync();
        TempData["Success"] = "บันทึกข้อมูลบัญชีแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    // Every account can change its own password (needed after Admin sets a temporary one).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordForm form)
    {
        if (!ModelState.IsValid)
            TempData["Error"] = string.Join(" · ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct());
        else if (await users.ChangePasswordAsync(UserId, form.Current, form.Next) is not { } user)
            TempData["Error"] = "รหัสผ่านปัจจุบันไม่ถูกต้อง";
        else
        {
            // This device stays signed in with the new stamp; every other device is signed out on its next request.
            await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                Nontiwat_7362_L002.Security.UserPrincipal.Create(user));
            TempData["Success"] = "เปลี่ยนรหัสผ่านแล้ว อุปกรณ์อื่นที่เข้าสู่ระบบไว้จะถูกออกจากระบบ";
        }
        return Redirect(Url.Action(nameof(Profile)) + "#change-password");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Avatar(string? id)
    {
        if (id is null || !Regex.IsMatch(id, @"\A[a-f0-9]{32}\.(jpg|png)\z")) return NotFound();
        var file = Path.Combine(env.ContentRootPath, "App_Data", "Avatars", id);
        if (!System.IO.File.Exists(file)) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(file, id.EndsWith(".png") ? "image/png" : "image/jpeg");
    }

    // Credit is only for booking, which is a customer action.
    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> Wallet() => View(await WalletModel(new()));

    private async Task<WalletPage> WalletModel(TopUpForm form) => new()
    {
        Balance = await db.Wallets.AsNoTracking().Where(x => x.UserId == UserId).Select(x => x.Balance).SingleOrDefaultAsync(),
        Form = form
    };

    // The full credit history, moved off the wallet page so that page stays about topping up.
    [Authorize(Policy = RolePolicies.Customer), HttpGet]
    public async Task<IActionResult> CreditHistory(int page = 1)
    {
        var mine = db.WalletEntries.AsNoTracking().Where(x => x.UserId == UserId);
        var total = await mine.CountAsync();
        var pages = Math.Max(1, (total + CreditHistoryPage.PageSize - 1) / CreditHistoryPage.PageSize);
        page = Math.Clamp(page, 1, Math.Max(pages, 1));
        var entries = await mine.OrderByDescending(x => x.Id).Skip((page - 1) * CreditHistoryPage.PageSize).Take(CreditHistoryPage.PageSize).ToListAsync();
        var balance = await db.Wallets.AsNoTracking().Where(x => x.UserId == UserId).Select(x => x.Balance).SingleOrDefaultAsync();
        return View(new CreditHistoryPage(balance, entries, page, pages, total));
    }

    [Authorize(Policy = RolePolicies.Customer), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TopUp([Bind(Prefix = "Form")] TopUpForm form)
    {
        if (!ModelState.IsValid) return View("Wallet", await WalletModel(form));
        try { await walletService.TopUpAsync(UserId, form); TempData["Success"] = "เพิ่มเครดิตเข้าบัญชีแล้ว"; }
        catch (ArgumentException e) { ModelState.AddModelError("", e.Message); return View("Wallet", await WalletModel(form)); }
        return RedirectToAction(nameof(Wallet));
    }
}
