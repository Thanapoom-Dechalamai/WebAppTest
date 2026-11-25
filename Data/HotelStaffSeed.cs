using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
namespace Nontiwat_7362_L002.Data;

// Ten demo HotelStaff accounts (hotelstaff01..10@lodgo.test, password LodgoDemo!2026).
// Hotels are split between them by province so each person looks after one area.
// Runs on every start but only adds what is missing: it never changes an existing account,
// and it only gives a team to hotels that have nobody assigned yet.
public static class HotelStaffSeed
{
    // PasswordHasher output for "LodgoDemo!2026" (same as Database/seed-hotel-owner.sql).
    private const string DemoPasswordHash = "AQAAAAIAAYagAAAAEFAUmeIYEYV7aJjGEHGJ6DJsKneOUXgEUc2d1jOJ75OQPCDnkdnRCkWGtuIjRDvI/Q==";
    private static readonly (string First, string Last)[] Names =
    [
        ("สมชาย", "ใจดี"), ("สุดารัตน์", "แสงทอง"), ("ธนพล", "วงศ์ไทย"), ("กมลวรรณ", "ศรีสุข"), ("ณัฐวุฒิ", "บุญมา"),
        ("พิมพ์ชนก", "รุ่งเรือง"), ("วีรยุทธ", "ทองดี"), ("อรอุมา", "จันทร์เพ็ญ"), ("ปิยะพงษ์", "สายสุวรรณ"), ("ชลธิชา", "มีสุข")
    ];

    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        var staffIds = new List<int>();
        for (var i = 0; i < Names.Length; i++)
        {
            var email = $"hotelstaff{i + 1:00}@lodgo.test";
            var normalized = email.ToUpperInvariant();
            var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalized);
            if (user is null)
            {
                user = new ApplicationUser { FirstName = Names[i].First, LastName = Names[i].Last, Email = email,
                    NormalizedEmail = normalized, PasswordHash = DemoPasswordHash, Role = AppRoles.HotelOwner };
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            if (user.Role == AppRoles.HotelOwner) staffIds.Add(user.Id);
        }
        if (staffIds.Count == 0) return;

        var staffed = await db.HotelAssignments.Select(x => x.HotelId).Distinct().ToListAsync();
        var hotels = await db.Hotels.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.City).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.City }).ToListAsync();
        var cities = hotels.Select(x => x.City).Distinct().ToList();
        var added = false;
        foreach (var hotel in hotels.Where(x => !staffed.Contains(x.Id)))
        {
            var staffId = staffIds[cities.IndexOf(hotel.City) % staffIds.Count];
            db.HotelAssignments.Add(new HotelAssignment { HotelId = hotel.Id, OwnerUserId = staffId, UpdatedAtUtc = DateTime.UtcNow });
            added = true;
        }
        if (added) await db.SaveChangesAsync();
    }
}
