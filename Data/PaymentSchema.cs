using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;

// Design 2026-11-22: bank transfer with a slip checked by Staff, commission locked per booking,
// and hotel staff asking for a hotel with proof. Runs before the seeds (they read hotels and stay_orders).
public static class PaymentSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "PaymentMethod", "varchar(16) NOT NULL DEFAULT 'Credit'");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "PaymentRef", "varchar(64) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "SlipFile", "varchar(80) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "CommissionPercent", "decimal(5,2) NOT NULL DEFAULT 10.00");
        // When the request reached the hotel: booking time for credit, Staff approval time for a transfer.
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "HotelSentAtUtc", "datetime(6) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotels", "CommissionPercent", "decimal(5,2) NOT NULL DEFAULT 10.00");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_access_requests (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, UserId int NOT NULL, HotelId varchar(32) NOT NULL,
                Message varchar(500) NULL, ProofFile varchar(80) NOT NULL, Status varchar(12) NOT NULL,
                ReviewerId int NULL, ReviewNote varchar(300) NULL, CreatedAtUtc datetime(6) NOT NULL, ReviewedAtUtc datetime(6) NULL,
                KEY IX_hotel_access_requests_status (Status, CreatedAtUtc), KEY IX_hotel_access_requests_user (UserId),
                FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE,
                FOREIGN KEY (HotelId) REFERENCES hotels(Id) ON DELETE CASCADE,
                FOREIGN KEY (ReviewerId) REFERENCES users(Id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }
}
