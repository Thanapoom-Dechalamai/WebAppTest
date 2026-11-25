namespace Nontiwat_7362_L002.Models;

public class ApplicationUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
    // Suspended accounts cannot sign in (set by Admin, see UserService.SuspendAsync).
    public bool IsSuspended { get; set; }
    public string? SuspendedReason { get; set; }
    // Changes on every password reset or change; sign-in cookies made with an older value stop working (UserPrincipal).
    public string? SecurityStamp { get; set; }
}
