using System.Security.Claims;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext db, UserService users, HotelOwnershipService ownership) : Controller
{
    // Hotel checkboxes for the add/edit user forms: ticked hotels first, then by name.
    private async Task<T> WithHotels<T>(T form) where T : IHotelPickerForm
    {
        var ticked = form.HotelIds.ToHashSet();
        form.HotelOptions = (await db.Hotels.AsNoTracking().Where(h => h.IsActive || ticked.Contains(h.Id))
            .Select(h => new HotelOption(h.Id, h.Name, h.IsActive ? h.City : h.City + " · ซ่อนจากหน้าเว็บ")).ToListAsync())
            .OrderBy(h => ticked.Contains(h.Id) ? 0 : 1).ThenBy(h => h.Name).ToList();
        return form;
    }

    [HttpGet]
    public IActionResult Index() => View();

    // Everything written to the database is logged automatically (see ApplicationDbContext).
    // This page lets Admin find who changed what and when.
    [HttpGet]
    public async Task<IActionResult> AuditLogs(string? table, string? kind, string? actor, string? record, DateTime? from, DateTime? to, int pageNo = 1)
    {
        const int size = 50;
        var logs = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(table)) logs = logs.Where(x => x.TableName == table);
        if (kind is "Added" or "Modified" or "Deleted") logs = logs.Where(x => x.Action == kind); else kind = null;
        if (!string.IsNullOrWhiteSpace(record))
        {
            record = record.Trim();
            var key = record.StartsWith("LG", StringComparison.OrdinalIgnoreCase) && int.TryParse(record[2..], out var number)
                ? number.ToString() : record;
            logs = logs.Where(x => x.RecordId == key);
        }
        if (!string.IsNullOrWhiteSpace(actor))
        {
            actor = actor.Trim();
            var name = actor;
            var ids = await db.Users.AsNoTracking()
                .Where(u => u.Email.Contains(name) || u.FirstName.Contains(name) || u.LastName.Contains(name))
                .Select(u => u.Id).Take(200).ToListAsync();
            logs = logs.Where(x => x.ActorUserId != null && ids.Contains(x.ActorUserId.Value));
        }
        // Dates are picked in Thai time; the log is stored in UTC.
        if (from is not null) { var start = from.Value.Date.AddHours(-7); logs = logs.Where(x => x.CreatedAtUtc >= start); }
        if (to is not null) { var end = to.Value.Date.AddDays(1).AddHours(-7); logs = logs.Where(x => x.CreatedAtUtc < end); }
        var total = await logs.CountAsync();
        pageNo = Math.Clamp(pageNo, 1, Math.Max(1, (total + size - 1) / size));
        var items = await logs.OrderByDescending(x => x.Id).Skip((pageNo - 1) * size).Take(size).ToListAsync();
        var actorIds = items.Where(x => x.ActorUserId != null).Select(x => x.ActorUserId!.Value).Distinct().ToList();
        var people = await db.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.Email }).ToDictionaryAsync(u => u.Id);
        var rows = items.Select(x => x.ActorUserId is int id && people.TryGetValue(id, out var person)
            ? new AuditLogRow(x, person.Name, person.Email) : new AuditLogRow(x, null, null)).ToList();
        var tables = await db.AuditLogs.AsNoTracking().Select(x => x.TableName).Distinct().OrderBy(x => x).ToListAsync();
        return View(new AuditLogPage(rows, total, pageNo, size, tables, table, kind, actor, record, from, to));
    }
    [HttpGet]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        return View(new EditUserViewModel { Id = user.Id, FirstName = user.FirstName,
            LastName = user.LastName, Email = user.Email, Role = user.Role });
    }

    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
        {
            TempData["Error"] = "ไม่สามารถลบบัญชีที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(UserList));
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = (await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE Id = {id} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (user is null) return NotFound();
        if (await db.Orders.AnyAsync(x => x.UserId == id) || await db.WalletEntries.AnyAsync(x => x.UserId == id) || await db.HotelAssignments.AnyAsync(x => x.OwnerUserId == id) || await db.HotelOwnerEntries.AnyAsync(x => x.OwnerUserId == id))
        {
            TempData["Error"] = "บัญชีนี้มีประวัติการเงินหรือการจอง จึงลบไม่ได้เพื่อเก็บประวัติรายการ";
            return RedirectToAction(nameof(UserList));
        }
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"ลบข้อมูล {user.Email} สำเร็จ";
        return RedirectToAction(nameof(UserList));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        var hotelIds = await db.HotelAssignments.AsNoTracking().Where(x => x.OwnerUserId == id).Select(x => x.HotelId).ToListAsync();
        return View(await WithHotels(new EditUserViewModel { Id = user.Id, FirstName = user.FirstName,
            LastName = user.LastName, Email = user.Email, Role = user.Role, HotelIds = hotelIds }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel data, [FromServices] SupportTicketService tickets)
    {
        // Ticked hotels only mean something for HotelStaff; to change the role away, untick every hotel in the same save.
        if (data.Role != AppRoles.HotelOwner && data.HotelIds.Count > 0)
            ModelState.AddModelError(nameof(data.HotelIds), HotelPicker.RoleOnlyMessage + " ถ้าจะเปลี่ยนบทบาท ให้เอาเครื่องหมายออกจากทุกโรงแรมก่อน");
        if (!ModelState.IsValid) return View(await WithHotels(data));
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = (await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE Id = {data.Id} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (user is null) return NotFound();
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == data.Id.ToString() && data.Role != "Admin")
            ModelState.AddModelError(nameof(data.Role), "ไม่สามารถลดสิทธิ์ผู้ดูแลที่กำลังใช้งานอยู่ได้");
        var normalized = UserService.NormalizeEmail(data.Email);
        if (await db.Users.AnyAsync(x => x.Id != data.Id && x.NormalizedEmail == normalized))
            ModelState.AddModelError(nameof(data.Email), "อีเมลนี้มีผู้ใช้งานแล้ว");
        if (user.Role == AppRoles.HotelOwner || data.Role == AppRoles.HotelOwner)
        {
            var hotelError = await ownership.ReplaceHotelsAsync(user.Id, data.Role == AppRoles.HotelOwner ? data.HotelIds : []);
            if (hotelError is not null) ModelState.AddModelError(nameof(data.HotelIds), hotelError);
        }
        if (!ModelState.IsValid) return View(await WithHotels(data));
        user.FirstName = data.FirstName.Trim();
        user.LastName = data.LastName.Trim();
        user.Email = data.Email.Trim();
        user.NormalizedEmail = normalized;
        user.Role = data.Role;
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException e) when (e.InnerException is MySqlConnector.MySqlException { Number: 1062 })
        {
            ModelState.AddModelError(nameof(data.Email), "อีเมลนี้มีผู้ใช้งานแล้ว");
            return View(await WithHotels(data));
        }
        if (user.Role == AppRoles.HotelOwner && data.HotelIds.Count == 0) TempData["Error"] = $"{user.Email}: {HotelPicker.NoHotelWarning}";
        var released = await tickets.ReleaseIfCannotOwnAsync(ActorId, user.Id);
        TempData["Success"] = $"แก้ไขข้อมูล {user.Email} สำเร็จ" + ReleasedNote(released);
        return RedirectToAction(nameof(UserList));
    }

    // Told to the admin when an account's open tickets went back to "ยังไม่มีคนรับ" (B5).
    private static string ReleasedNote(int released) => released > 0 ? $" · ปล่อย ticket ที่ดูแลอยู่ {released} เรื่องกลับไปที่ \"ยังไม่มีคนรับ\"" : "";

    // ---------- Suspend / reset password ----------
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(int id, string? reason, [FromServices] SupportTicketService tickets)
    {
        var error = await users.SuspendAsync(ActorId, id, reason ?? "");
        if (error is null) TempData["Success"] = "ระงับบัญชีแล้ว ผู้ใช้จะถูกออกจากระบบในคำขอถัดไป การจองและเครดิตยังอยู่ครบ"
            + ReleasedNote(await tickets.ReleaseIfCannotOwnAsync(ActorId, id));
        else TempData["Error"] = error;
        return RedirectToAction(nameof(UserList));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unsuspend(int id)
    {
        var error = await users.UnsuspendAsync(id);
        if (error is null) TempData["Success"] = "ยกเลิกการระงับแล้ว ผู้ใช้เข้าสู่ระบบได้ตามปกติ";
        else TempData["Error"] = error;
        return RedirectToAction(nameof(UserList));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var email = await db.Users.Where(x => x.Id == id).Select(x => x.Email).SingleOrDefaultAsync();
        if (email is null) return NotFound();
        TempData["TempPassword"] = await users.ResetPasswordAsync(id);
        TempData["TempPasswordFor"] = email;
        return RedirectToAction(nameof(UserList));
    }

    [HttpGet]
    public async Task<IActionResult> AddUser() => View(await WithHotels(new AddUserViewModel()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(AddUserViewModel data)
    {
        if (data.Role != AppRoles.HotelOwner && data.HotelIds.Count > 0)
            ModelState.AddModelError(nameof(data.HotelIds), HotelPicker.RoleOnlyMessage);
        var wanted = data.HotelIds.Distinct().ToList();
        if (wanted.Count > 0 && await db.Hotels.CountAsync(h => h.IsActive && wanted.Contains(h.Id)) != wanted.Count)
            ModelState.AddModelError(nameof(data.HotelIds), "ไม่พบโรงแรมบางแห่งที่เลือก กรุณาเลือกใหม่");
        if (!ModelState.IsValid) return View(await WithHotels(data));
        var result = await users.CreateAsync(data, data.Role);
        if (result.DuplicateEmail)
        {
            ModelState.AddModelError(nameof(data.Email), "อีเมลนี้มีผู้ใช้งานแล้ว");
            return View(await WithHotels(data));
        }
        if (data.Role == AppRoles.HotelOwner && wanted.Count > 0)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM users WHERE Id = {result.User!.Id} FOR UPDATE");
            await ownership.ReplaceHotelsAsync(result.User.Id, wanted);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        if (data.Role == AppRoles.HotelOwner && wanted.Count == 0) TempData["Error"] = $"{result.User!.Email}: {HotelPicker.NoHotelWarning}";
        TempData["Success"] = data.Role == AppRoles.HotelOwner && wanted.Count > 0
            ? $"เพิ่มสมาชิก {result.User!.Email} สำเร็จ และให้ดูแล {wanted.Count} โรงแรม" : $"เพิ่มสมาชิก {result.User!.Email} สำเร็จ";
        return RedirectToAction(nameof(UserList));
    }
    [HttpGet]
    public async Task<IActionResult> UserList(string? search, string? role, string? status, int page = 1)
    {
        var filter = UserDirectoryQuery.Normalize(search, role, status, page);
        var query = filter.Apply(db.Users.AsNoTracking());
        const int pageSize = 20;
        var total = await query.CountAsync();
        page = filter.ClampPage(total, pageSize);
        var rows = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new UserRowViewModel(x.Id, x.Email, x.FirstName, x.LastName, x.Role, x.CreatedAtUtc) { IsSuspended = x.IsSuspended, SuspendedReason = x.SuspendedReason,
                HotelCount = db.HotelAssignments.Count(a => a.OwnerUserId == x.Id), LastLoginUtc = x.LastLoginUtc })
            .ToListAsync();
        return View(new UserListViewModel { Search = filter.Search, RoleFilter = filter.Role, StatusFilter = filter.Status,
            Page = page, PageCount = UserDirectoryQuery.PageCount(total, pageSize), Total = total, Users = rows });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(ChangeRoleViewModel data, [FromServices] SupportTicketService tickets)
    {
        if (!ModelState.IsValid) return BadRequest();
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == data.Id.ToString() && data.Role != "Admin")
        {
            TempData["Error"] = "ไม่สามารถลดสิทธิ์ผู้ดูแลที่กำลังใช้งานอยู่ได้";
            return RedirectToAction(nameof(UserList));
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = (await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE Id = {data.Id} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (user?.Role == AppRoles.HotelOwner && data.Role != AppRoles.HotelOwner && await db.HotelAssignments.AnyAsync(x => x.OwnerUserId == data.Id))
        { TempData["Error"] = "กรุณาโอนโรงแรมให้เจ้าของคนใหม่ก่อนเปลี่ยนบทบาท"; return RedirectToAction(nameof(UserList)); }
        if (user is null) return NotFound();
        user.Role = data.Role;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = $"บันทึกสิทธิ์ของ {user.Email} แล้ว" + ReleasedNote(await tickets.ReleaseIfCannotOwnAsync(ActorId, user.Id));
        return RedirectToAction(nameof(UserList));
    }
}
