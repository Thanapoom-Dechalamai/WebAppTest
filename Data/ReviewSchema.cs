using Microsoft.EntityFrameworkCore;
namespace Nontiwat_7362_L002.Data;
public static class ReviewSchema
{
    public static Task EnsureAsync(ApplicationDbContext db) => db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS hotel_reviews (
            Id int NOT NULL AUTO_INCREMENT PRIMARY KEY, HotelId varchar(32) NOT NULL, OrderId int NULL, UserId int NULL,
            ReviewerName varchar(80) NOT NULL, ReviewerCountry varchar(40) NOT NULL, TravelerType varchar(20) NOT NULL,
            Rating int NOT NULL, Cleanliness int NOT NULL, Location int NOT NULL, Service int NOT NULL, Value int NOT NULL,
            Title varchar(120) NOT NULL, Body varchar(1500) NOT NULL, RoomName varchar(100) NOT NULL,
            StayedOn datetime(6) NOT NULL, IsSample tinyint(1) NOT NULL, CreatedAtUtc datetime(6) NOT NULL,
            UNIQUE KEY UX_hotel_reviews_order (OrderId),
            KEY IX_hotel_reviews_hotel (HotelId, Rating),
            FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE,
            FOREIGN KEY (OrderId) REFERENCES stay_orders(Id) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """);
}
