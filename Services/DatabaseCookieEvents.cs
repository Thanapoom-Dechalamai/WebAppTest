using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;

namespace Nontiwat_7362_L002.Services;

public class DatabaseCookieEvents(ApplicationDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        // A suspended account is signed out on its next request, not when the cookie expires.
        // So is a cookie made before the password was reset or changed elsewhere (its security stamp is old).
        if (user is null || user.IsSuspended || !AppRoles.IsValid(user.Role) || !UserPrincipal.StampMatches(user.SecurityStamp, context.Principal!))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        if (context.Principal!.FindFirstValue(ClaimTypes.Role) != user.Role)
        {
            context.ReplacePrincipal(UserPrincipal.Create(user));
            context.ShouldRenew = true;
        }
    }
}
