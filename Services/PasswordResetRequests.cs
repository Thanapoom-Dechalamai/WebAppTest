using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;

namespace Nontiwat_7362_L002.Services;

// Pure rules for "ลืมรหัสผ่าน" (D, 2026-11-25). The site cannot send e-mail, so a request becomes a bell notification for
// every active Admin, who resets the password and passes it on. At most one request per account per hour.
public static class PasswordResetRules
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(1);
    public const string Title = "คำขอรีเซ็ตรหัสผ่าน";

    public static bool MaySend(DateTime? lastRequestUtc, DateTime nowUtc) => lastRequestUtc is null || nowUtc - lastRequestUtc.Value >= Cooldown;

    public static string Body(string email) => $"{email} ขอรีเซ็ตรหัสผ่านจากหน้าลืมรหัสผ่าน ตรวจตัวตนก่อนรีเซ็ตและแจ้งรหัสชั่วคราว";

    public static string AdminUrl(string email) => "/Admin/UserList?search=" + Uri.EscapeDataString(email);
}

// The page always answers the same way, whether or not the e-mail has an account, so it cannot be used to find accounts.
public class PasswordResetRequests(ApplicationDbContext db, IThaiClock clock)
{
    public async Task RequestAsync(string email)
    {
        var normalized = UserService.NormalizeEmail(email);
        var user = await db.Users.AsNoTracking().Where(x => x.NormalizedEmail == normalized).Select(x => new { x.Id, x.Email }).SingleOrDefaultAsync();
        if (user is null) return;
        var body = PasswordResetRules.Body(user.Email);
        var now = clock.UtcNow;
        var last = await db.Notifications.AsNoTracking().Where(x => x.Title == PasswordResetRules.Title && x.Body == body)
            .MaxAsync(x => (DateTime?)x.CreatedAtUtc);
        if (!PasswordResetRules.MaySend(last, now)) return;
        var admins = await db.Users.AsNoTracking().Where(x => x.Role == AppRoles.Admin && !x.IsSuspended).Select(x => x.Id).ToListAsync();
        foreach (var admin in admins)
            db.Notifications.Add(new Notification
            {
                UserId = admin, Title = PasswordResetRules.Title, Body = body, Url = PasswordResetRules.AdminUrl(user.Email), CreatedAtUtc = now
            });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO audit_logs (ActorUserId,TableName,Action,RecordId,CreatedAtUtc) VALUES ({null},'authentication','PasswordResetRequested',{user.Id.ToString()},UTC_TIMESTAMP(6))");
    }
}
