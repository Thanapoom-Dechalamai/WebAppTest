using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class CartSchema
{
    public static Task EnsureAsync(ApplicationDbContext db) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS cart_items (
            Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, UserId int NOT NULL, RequestId varchar(36) NOT NULL,
            HotelId varchar(32) NOT NULL, RoomType varchar(32) NOT NULL,
            CheckIn datetime(6) NOT NULL, CheckOut datetime(6) NOT NULL, Rooms int NOT NULL, Guests int NOT NULL,
            CreatedAtUtc datetime(6) NOT NULL,
            UNIQUE KEY UX_cart_items_request (RequestId),
            KEY IX_cart_items_user (UserId),
            FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
