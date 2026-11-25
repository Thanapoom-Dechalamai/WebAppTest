using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class RoleSchema
{
    public static Task EnsureAsync(ApplicationDbContext db) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS promotions (
            Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
            Title varchar(120) NOT NULL, Details varchar(1000) NOT NULL,
            StartsOn datetime(6) NOT NULL, EndsOn datetime(6) NOT NULL,
            IsActive tinyint(1) NOT NULL DEFAULT 1, UpdatedByUserId int NOT NULL,
            UpdatedAtUtc datetime(6) NOT NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
