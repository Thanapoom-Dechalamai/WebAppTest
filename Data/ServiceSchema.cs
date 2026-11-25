using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;

// Round 2 (design 2026-11-21): wishlist, special requests, hotel replies to reviews and light support.
// Safe to run on every start.
public static class ServiceSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await SchemaHelpers.AddColumnIfMissingAsync(db, "stay_orders", "SpecialRequest", "varchar(300) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "cart_items", "SpecialRequest", "varchar(300) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotel_reviews", "ReplyBody", "varchar(1000) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotel_reviews", "ReplyByUserId", "int NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotel_reviews", "ReplyAtUtc", "datetime(6) NULL");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS wishlists (
                UserId int NOT NULL, HotelId varchar(32) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                PRIMARY KEY (UserId, HotelId),
                FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE,
                FOREIGN KEY (HotelId) REFERENCES hotels(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS support_cases (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, OrderId int NULL, CustomerId int NOT NULL,
                Topic varchar(20) NOT NULL, Subject varchar(120) NOT NULL, Status varchar(12) NOT NULL,
                CreatedAtUtc datetime(6) NOT NULL, UpdatedAtUtc datetime(6) NOT NULL,
                KEY IX_support_cases_status (Status, UpdatedAtUtc), KEY IX_support_cases_customer (CustomerId),
                FOREIGN KEY (CustomerId) REFERENCES users(Id) ON DELETE CASCADE,
                FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS support_messages (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, CaseId int NOT NULL, AuthorUserId int NOT NULL,
                Body varchar(2000) NOT NULL, FromStaff tinyint(1) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                KEY IX_support_messages_case (CaseId),
                FOREIGN KEY (CaseId) REFERENCES support_cases(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        foreach (var (name, type) in new[] { ("HotelId", "varchar(32)"), ("RoomType", "varchar(32)"),
                     ("CheckIn", "date"), ("CheckOut", "date"), ("Rooms", "int"), ("Guests", "int"),
                     ("PromotionCode", "varchar(40)"), ("SpecialRequest", "varchar(300)") })
            await SchemaHelpers.AddColumnIfMissingAsync(db, "support_cases", name, type + " NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "support_messages", "Attachment", "varchar(40) NULL");

        // Tickets (2026-11-25): who owns a ticket, and the history of that. Deleting a user keeps the ticket
        // and its history (SET NULL), so purge_test_data.py can still remove test staff.
        await SchemaHelpers.AddColumnIfMissingAsync(db, "support_cases", "AssigneeUserId", "int NULL");
        await SchemaHelpers.AddConstraintIfMissingAsync(db, "support_cases", "FK_support_cases_assignee",
            "FOREIGN KEY (AssigneeUserId) REFERENCES users(Id) ON DELETE SET NULL");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS support_assignments (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, CaseId int NOT NULL,
                ActorUserId int NULL, FromUserId int NULL, ToUserId int NULL,
                Kind varchar(12) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                KEY IX_support_assignments_case (CaseId, Id),
                FOREIGN KEY (CaseId) REFERENCES support_cases(Id) ON DELETE CASCADE,
                FOREIGN KEY (ActorUserId) REFERENCES users(Id) ON DELETE SET NULL,
                FOREIGN KEY (FromUserId) REFERENCES users(Id) ON DELETE SET NULL,
                FOREIGN KEY (ToUserId) REFERENCES users(Id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);

        // Quotes (2026-11-25): support offers a price in a ticket; the customer accepts it and pays, or declines.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS support_quotes (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, CaseId int NOT NULL, CustomerId int NOT NULL, CreatedByUserId int NULL,
                RequestId varchar(36) NOT NULL, HotelId varchar(32) NOT NULL, HotelName varchar(120) NOT NULL,
                RoomType varchar(32) NOT NULL, RoomName varchar(100) NOT NULL, CheckIn date NOT NULL, CheckOut date NOT NULL,
                Rooms int NOT NULL, Guests int NOT NULL, PromotionCode varchar(40) NULL, SpecialRequest varchar(300) NULL,
                Subtotal decimal(12,2) NOT NULL, Discount decimal(12,2) NOT NULL, Total decimal(12,2) NOT NULL,
                Status varchar(12) NOT NULL, ExpiresAtUtc datetime(6) NOT NULL, OrderId int NULL,
                CreatedAtUtc datetime(6) NOT NULL, DecidedAtUtc datetime(6) NULL,
                KEY IX_support_quotes_case (CaseId, Id), UNIQUE KEY UX_support_quotes_request (RequestId),
                FOREIGN KEY (CaseId) REFERENCES support_cases(Id) ON DELETE CASCADE,
                FOREIGN KEY (CustomerId) REFERENCES users(Id) ON DELETE CASCADE,
                FOREIGN KEY (CreatedByUserId) REFERENCES users(Id) ON DELETE SET NULL,
                FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }
}
