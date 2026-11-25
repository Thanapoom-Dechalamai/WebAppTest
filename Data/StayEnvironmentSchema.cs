using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class StayEnvironmentSchema
{
    public static Task EnsureAsync(ApplicationDbContext db) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS stay_environments (
            HotelId varchar(32) NOT NULL PRIMARY KEY,
            Category varchar(20) NOT NULL,
            ViewType varchar(20) NOT NULL,
            HasPool tinyint(1) NULL,
            SeaDistanceKm decimal(8,3) NULL,
            ForestDistanceKm decimal(8,3) NULL,
            ViewDescription varchar(600) NULL,
            UpdatedAtUtc datetime(6) NOT NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
