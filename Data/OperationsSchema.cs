using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;

// Tables for the booking history, in-site notifications and the room calendar.
public static class OperationsSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        // Bookings made before this column existed keep working: the room type is filled in from the room name.
        if (await SchemaHelpers.ColumnCountAsync(db, "stay_orders", "RoomType") == 0)
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE stay_orders ADD COLUMN RoomType varchar(32) NULL AFTER RoomName, ADD INDEX IX_stay_orders_room_nights (HotelId, RoomType, CheckIn, CheckOut)");
        await db.Database.ExecuteSqlRawAsync("UPDATE stay_orders o JOIN room_types r ON r.Name = o.RoomName SET o.RoomType = r.Id WHERE o.RoomType IS NULL");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS booking_events (
                Id bigint NOT NULL AUTO_INCREMENT PRIMARY KEY, OrderId int NOT NULL,
                Status varchar(20) NOT NULL, Note varchar(255) NOT NULL,
                ActorUserId int NULL, ActorLabel varchar(120) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
                KEY IX_booking_events_order (OrderId),
                FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS notifications (
                Id bigint NOT NULL AUTO_INCREMENT PRIMARY KEY, UserId int NOT NULL, OrderId int NULL,
                Title varchar(120) NOT NULL, Body varchar(255) NOT NULL, Url varchar(255) NOT NULL,
                IsRead tinyint(1) NOT NULL DEFAULT 0, CreatedAtUtc datetime(6) NOT NULL,
                KEY IX_notifications_user (UserId, IsRead),
                FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS room_allotments (
                HotelId varchar(32) NOT NULL, RoomType varchar(32) NOT NULL, Date date NOT NULL,
                Rooms int NOT NULL, Closed tinyint(1) NOT NULL DEFAULT 0, UpdatedAtUtc datetime(6) NOT NULL,
                PRIMARY KEY (HotelId, RoomType, Date)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }
}
