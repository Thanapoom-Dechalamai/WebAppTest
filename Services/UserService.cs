using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services;

public sealed record CreateUserResult(ApplicationUser? User, bool DuplicateEmail);

public class UserService(ApplicationDbContext db, IPasswordHasher<ApplicationUser> passwordHasher)
{
    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public async Task<CreateUserResult> CreateAsync(RegisterViewModel data, string role = "User")
    {
        if (!Nontiwat_7362_L002.Security.AppRoles.IsValid(role)) throw new ArgumentException("Invalid role.", nameof(role));
        var normalized = NormalizeEmail(data.Email);
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == normalized))
            return new(null, true);

        var user = new ApplicationUser
        {
            Email = data.Email.Trim(), NormalizedEmail = normalized,
            FirstName = data.FirstName.Trim(), LastName = data.LastName.Trim(), Role = role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, data.Password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
            return new(user, false);
        }
        catch (DbUpdateException e) when (e.InnerException is MySqlException { Number: 1062 })
        {
            // The unique index also protects two simultaneous registrations.
            db.Entry(user).State = EntityState.Detached;
            return new(null, true);
        }
    }

    public async Task<ApplicationUser?> AuthenticateAsync(string email, string password)
    {
        var normalized = NormalizeEmail(email);
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized);
        if (user is null || !Nontiwat_7362_L002.Security.AppRoles.IsValid(user.Role)) { await AuthLog(null,"LoginFailed"); return null; }
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) { await AuthLog(null,"LoginFailed"); return null; }
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await db.SaveChangesAsync();
        }
        // A suspended account is turned away by the caller: log it as blocked, not as a successful login.
        await AuthLog(user.Id, user.IsSuspended ? "LoginBlocked" : "LoginSuccess");
        return user;
    }
    // ---------- Admin: suspend / reset password; everyone: change own password ----------
    public static string GenerateTemporaryPassword() => PasswordRules.GenerateTemporaryPassword();

    // Returns null when done, otherwise a Thai message saying why it was refused.
    // Suspending never touches bookings or credit: those stay as they are.
    public async Task<string?> SuspendAsync(int actorId, int userId, string reason)
    {
        reason = (reason ?? "").Trim();
        if (reason.Length == 0) return "กรุณาระบุเหตุผลที่ระงับบัญชี";
        if (reason.Length > 200) reason = reason[..200];
        if (actorId == userId) return "ระงับบัญชีของตัวเองไม่ได้";
        // One transaction: the active admins are locked before counting, so two admins suspending each other at the same
        // moment run one after the other and the second is refused (at least one active admin always remains).
        await using var transaction = await db.Database.BeginTransactionAsync();
        var activeAdmins = await db.Users.FromSqlInterpolated(
            $"SELECT * FROM users WHERE Role = {Nontiwat_7362_L002.Security.AppRoles.Admin} AND IsSuspended = 0 ORDER BY Id FOR UPDATE").ToListAsync();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user is null) return "ไม่พบบัญชีนี้";
        if (user.IsSuspended) return "บัญชีนี้ถูกระงับอยู่แล้ว";
        if (user.Role == Nontiwat_7362_L002.Security.AppRoles.Admin && activeAdmins.Count <= 1)
            return "ต้องมีผู้ดูแลระบบที่ใช้งานได้อย่างน้อย 1 คน";
        if (user.Role == Nontiwat_7362_L002.Security.AppRoles.HotelOwner)
        {
            // A hotel whose only team member is this person would be left with nobody to confirm bookings.
            var soleHotel = await (from mine in db.HotelAssignments
                                   where mine.OwnerUserId == userId
                                      && !(from other in db.HotelAssignments
                                           join u in db.Users on other.OwnerUserId equals u.Id
                                           where other.HotelId == mine.HotelId && other.OwnerUserId != userId
                                              && !u.IsSuspended && u.Role == Nontiwat_7362_L002.Security.AppRoles.HotelOwner
                                           select other).Any()
                                   join h in db.Hotels on mine.HotelId equals h.Id
                                   select h.Name).FirstOrDefaultAsync();
            if (soleHotel is not null) return $"เป็นพนักงานคนเดียวของ {soleHotel} เพิ่มคนอื่นเข้าทีมก่อนจึงจะระงับได้";
        }
        user.IsSuspended = true;
        user.SuspendedReason = reason;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<string?> UnsuspendAsync(int userId)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user is null) return "ไม่พบบัญชีนี้";
        if (!user.IsSuspended) return "บัญชีนี้ไม่ได้ถูกระงับ";
        user.IsSuspended = false;
        user.SuspendedReason = null;
        await db.SaveChangesAsync();
        return null;
    }

    // Sets a new random password and returns it once; only its hash is stored.
    public async Task<string> ResetPasswordAsync(int userId)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId) ?? throw new ArgumentException("ไม่พบบัญชีนี้");
        var temporary = GenerateTemporaryPassword();
        user.PasswordHash = passwordHasher.HashPassword(user, temporary);
        user.SecurityStamp = Nontiwat_7362_L002.Security.UserPrincipal.NewStamp(); // every device of this user signs out
        await db.SaveChangesAsync();
        return temporary;
    }

    // Null when the current password is wrong. A new security stamp signs out the user's other devices; the caller
    // signs the current device in again with the returned account (MemberController.ChangePassword).
    public async Task<ApplicationUser?> ChangePasswordAsync(int userId, string current, string next)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, current) == PasswordVerificationResult.Failed) return null;
        user.PasswordHash = passwordHasher.HashPassword(user, next);
        user.SecurityStamp = Nontiwat_7362_L002.Security.UserPrincipal.NewStamp();
        await db.SaveChangesAsync();
        return user;
    }

    private Task<int> AuthLog(int? id,string action) => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO audit_logs (ActorUserId,TableName,Action,RecordId,CreatedAtUtc) VALUES ({id},\"authentication\",{action},{id.ToString()},UTC_TIMESTAMP(6))");
}
