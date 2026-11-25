using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

public class AccountController(ApplicationDbContext db, UserService users, Nontiwat_7362_L002.Services.CatalogService catalog, IThaiClock clock) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ApplicationUser? user = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return View(new AccountIndexViewModel
        {
            Email = user?.Email ?? string.Empty,
            FullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}",
            Role = user?.Role ?? string.Empty,
            AccountType = HttpContext.Session.GetString("AccountType") ?? string.Empty,
            Promotions = await db.Promotions.AsNoTracking().Where(x=>x.IsActive && x.Code!=null && x.DiscountValue>0 && x.StartsOn<=clock.Today && x.EndsOn>=clock.Today).OrderBy(x=>x.Id).ToListAsync(),
            Hotels = await LoadHotelsAsync()
        });
    }
    private async Task<IReadOnlyList<HotelCardViewModel>> LoadHotelsAsync()
    {
        return await catalog.AllAsync();
    }
    public IActionResult MainMenu() => RedirectToAction(nameof(Index));
    public IActionResult Bookings() => RedirectToAction("Bookings", "Stays");
    public IActionResult Help() => View();
    public IActionResult Lab4() => View();

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool switchAccount = false, string? email = null)
    {
        if (User.Identity?.IsAuthenticated == true && !switchAccount)
            return RedirectToAction("Index", Nontiwat_7362_L002.Security.AppRoles.HomeController(Nontiwat_7362_L002.Security.AppRoles.Current(User)));

        ViewData["SwitchAccount"] = switchAccount;
        // The guide's "ลองเลย" buttons pre-fill the demo account's email (encoded by Razor when shown).
        email = email?.Trim();
        if (email?.Length > 200) email = email[..200];
        // asp-for reads the raw query value from ModelState before the model, so drop it to show the trimmed one.
        ModelState.Remove(nameof(email));
        return View(new LoginViewModel { ReturnUrl = returnUrl, Email = email ?? "" });
    }

    [HttpGet]
    public IActionResult SessionStatus() => Json(new
    {
        accountId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "" : ""
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel data)
    {
        if (!ModelState.IsValid) return View(data);
        var user = await users.AuthenticateAsync(data.Email, data.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
            return View(data);
        }
        if (user.IsSuspended)
        {
            ModelState.AddModelError(string.Empty, "บัญชีนี้ถูกระงับ กรุณาติดต่อผู้ดูแลระบบ");
            return View(data);
        }
        await SignInAsync(user, "เข้าสู่ระบบ");
        var signedInAt = clock.UtcNow;
        await db.Users.Where(x => x.Id == user.Id && (x.LastLoginUtc == null || x.LastLoginUtc < signedInAt))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.LastLoginUtc, signedInAt));
        return Url.IsLocalUrl(data.ReturnUrl)
            ? LocalRedirect(data.ReturnUrl!)
            : RedirectToAction("Index", Nontiwat_7362_L002.Security.AppRoles.HomeController(user.Role));
    }
    // "ลืมรหัสผ่าน": the request goes to the admins as a notification; the page answers the same for every e-mail (D).
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordForm form, [FromServices] PasswordResetRequests requests)
    {
        if (!ModelState.IsValid) return View(form);
        await requests.RequestAsync(form.Email);
        ViewData["Sent"] = true;
        return View(new ForgotPasswordForm());
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel data)
    {
        if (!ModelState.IsValid) return View(data);
        // Public registration always creates a regular member, even if extra fields are posted.
        var result = await users.CreateAsync(data);
        if (result.DuplicateEmail)
        {
            ModelState.AddModelError(nameof(data.Email), "อีเมลนี้มีผู้ใช้งานแล้ว");
            return View(data);
        }
        await SignInAsync(result.User!, "สมัครสมาชิก");
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if(int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var actor))
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO audit_logs (ActorUserId,TableName,Action,RecordId,CreatedAtUtc) VALUES ({actor},'authentication','Logout',{actor.ToString()},UTC_TIMESTAMP(6))");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData.Clear();
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
    private async Task SignInAsync(ApplicationUser user, string accountType)
    {
        HttpContext.Session.Clear();
        TempData.Clear();
        var principal = Nontiwat_7362_L002.Security.UserPrincipal.Create(user);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        HttpContext.User = principal;
        HttpContext.Session.SetString("AccountType", accountType);
    }
}
