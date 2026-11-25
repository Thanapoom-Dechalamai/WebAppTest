using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Security;

/// <summary>
/// The sign-in cookie's claims, built in one place (login, role refresh, password change). Besides id, e-mail and role it
/// carries the account's security stamp: when the stamp in the database changes (password reset or change), every cookie
/// made with the old stamp is rejected on its next request (DatabaseCookieEvents). Added 2026-11-25.
/// </summary>
public static class UserPrincipal
{
    public const string StampClaim = "lodgo:stamp";

    public static ClaimsPrincipal Create(ApplicationUser user) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(StampClaim, user.SecurityStamp ?? "")
    ], CookieAuthenticationDefaults.AuthenticationScheme));

    // A new random stamp; giving the user one signs out every device that still holds the old one.
    public static string NewStamp() => Guid.NewGuid().ToString("N");

    // Accounts that never had a stamp (before 2026-11-25) and cookies made before it existed both read as "".
    public static bool StampMatches(string? userStamp, ClaimsPrincipal cookie) =>
        string.Equals(userStamp ?? "", cookie.FindFirstValue(StampClaim) ?? "", StringComparison.Ordinal);
}
