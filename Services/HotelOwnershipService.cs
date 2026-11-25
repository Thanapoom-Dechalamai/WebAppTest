using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
namespace Nontiwat_7362_L002.Services;

// A hotel has a team of HotelStaff accounts. Each member sees and handles only
// bookings for the hotels they are on; adding someone never removes the others.
public class HotelOwnershipService(ApplicationDbContext db)
{
    public async Task AssignAsync(int actorId, string hotelId, int staffId)
    {
        if (!await db.Hotels.AnyAsync(h => h.Id == hotelId && h.IsActive)) throw new ArgumentException("ไม่พบโรงแรม");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId);
        if (actor?.Role != AppRoles.Admin) throw new UnauthorizedAccessException();
        // User row lock serializes team changes with role changes/deletion.
        var staff = (await db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE Id = {staffId} FOR UPDATE").ToListAsync()).SingleOrDefault();
        if (staff?.Role != AppRoles.HotelOwner) throw new ArgumentException("เลือกบัญชีที่มีบทบาทพนักงานโรงแรมเท่านั้น");
        if (await db.HotelAssignments.AnyAsync(x => x.HotelId == hotelId && x.OwnerUserId == staffId))
        { await tx.CommitAsync(); return; }
        db.HotelAssignments.Add(new HotelAssignment { HotelId = hotelId, OwnerUserId = staffId, UpdatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Makes the staff account's hotels exactly hotelIds. The caller holds the transaction and the user row lock,
    // and saves. Returns an error message, or null when every hotel exists.
    public async Task<string?> ReplaceHotelsAsync(int staffId, IReadOnlyCollection<string> hotelIds)
    {
        var wanted = hotelIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var current = await db.HotelAssignments.Where(x => x.OwnerUserId == staffId).ToListAsync();
        var assigned = current.Select(x => x.HotelId).ToList();
        var known = await db.Hotels.Where(h => (h.IsActive || assigned.Contains(h.Id)) && wanted.Contains(h.Id))
            .Select(h => h.Id).ToListAsync();
        if (known.Count != wanted.Count) return "ไม่พบโรงแรมบางแห่งที่เลือก กรุณาเลือกใหม่";
        db.HotelAssignments.RemoveRange(current.Where(x => !wanted.Contains(x.HotelId)));
        foreach (var id in wanted.Except(assigned))
            db.HotelAssignments.Add(new HotelAssignment { HotelId = id, OwnerUserId = staffId, UpdatedAtUtc = DateTime.UtcNow });
        return null;
    }

    public async Task RemoveAsync(int actorId, string hotelId, int staffId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var actor = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId);
        if (actor?.Role != AppRoles.Admin) throw new UnauthorizedAccessException();
        var row = await db.HotelAssignments.SingleOrDefaultAsync(x => x.HotelId == hotelId && x.OwnerUserId == staffId);
        if (row is null) { await tx.CommitAsync(); return; }
        db.HotelAssignments.Remove(row);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
