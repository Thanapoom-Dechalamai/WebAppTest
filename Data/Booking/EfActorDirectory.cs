using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services.Booking;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// The EF implementation of <see cref="IActorDirectory"/>: roles from the users table, and the label a booking's
/// history shows next to each action. The label is kept as text on the event, so it still reads correctly after
/// the person changes role or leaves.
/// </summary>
public sealed class EfActorDirectory(ApplicationDbContext db) : IActorDirectory
{
    public async Task<(string Label, string? Role)> DescribeAsync(int actorId)
    {
        if (actorId == BookingText.SystemActorId) return ("ระบบอัตโนมัติ", null);
        var user = await db.Users.AsNoTracking().Where(x => x.Id == actorId)
            .Select(x => new { x.FirstName, x.Role }).SingleOrDefaultAsync();
        if (user is null) return ("ไม่ทราบผู้ดำเนินการ", null);
        var label = user.Role switch
        {
            AppRoles.HotelOwner => "พนักงานโรงแรม · " + user.FirstName,
            AppRoles.Staff => "Lodgo support · " + user.FirstName,
            AppRoles.Admin => "ผู้ดูแลระบบ Lodgo",
            AppRoles.Customer => "ลูกค้า · " + user.FirstName,
            _ => "ทีม Lodgo · " + user.FirstName
        };
        return (BookingText.Cut(label, 120), user.Role);
    }

    public async Task<string?> EnsureHotelTeamMemberAsync(int actorId, IReadOnlyList<int> teamMemberIds)
    {
        var role = await db.Users.AsNoTracking().Where(x => x.Id == actorId).Select(x => x.Role).SingleOrDefaultAsync();
        if (role != AppRoles.Admin && (role != AppRoles.HotelOwner || !teamMemberIds.Contains(actorId)))
            throw new UnauthorizedAccessException("ไม่มีสิทธิ์จัดการโรงแรมนี้");
        return role;
    }

    public async Task<int?> HotelRecorderAsync(int actorId, string? actorRole, IReadOnlyList<int> teamMemberIds)
    {
        if (actorRole == AppRoles.HotelOwner) return actorId;
        return await db.Users.Where(x => teamMemberIds.Contains(x.Id) && x.Role == AppRoles.HotelOwner)
            .OrderBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync();
    }
}
