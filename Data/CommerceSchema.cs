using Microsoft.EntityFrameworkCore;

namespace Nontiwat_7362_L002.Data;

public static class CommerceSchema
{
    // Additive bootstrap for projects that previously used EnsureCreated (no migrations).
    // Never drops or alters the existing users table.
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS member_wallets (
              UserId int NOT NULL PRIMARY KEY, Balance decimal(12,2) NOT NULL DEFAULT 0,
              AvatarFile varchar(80) NULL,
              FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS stay_orders (
              Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, UserId int NOT NULL,
              RequestId varchar(36) NOT NULL, HotelId varchar(32) NOT NULL,
              HotelName varchar(120) NOT NULL, City varchar(80) NOT NULL, RoomName varchar(100) NOT NULL,
              CheckIn datetime(6) NOT NULL, CheckOut datetime(6) NOT NULL,
              Rooms int NOT NULL, Guests int NOT NULL, NightlyRate decimal(12,2) NOT NULL,
              Total decimal(12,2) NOT NULL, Status varchar(20) NOT NULL,
              CreatedAtUtc datetime(6) NOT NULL,
              UNIQUE KEY UX_order_request (UserId, RequestId),
              FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS wallet_entries (
              Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, UserId int NOT NULL,
              RequestId varchar(80) NOT NULL, Kind varchar(20) NOT NULL,
              Amount decimal(12,2) NOT NULL, BalanceAfter decimal(12,2) NOT NULL,
              Description varchar(255) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
              UNIQUE KEY UX_wallet_request (UserId, RequestId),
              FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }
}
