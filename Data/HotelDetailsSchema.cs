using Microsoft.EntityFrameworkCore;

namespace Nontiwat_7362_L002.Data;

public static class HotelDetailsSchema
{
    public static async Task EnsureAsync(ApplicationDbContext db)
    {
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotels", "Description", "varchar(2500) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotels", "Amenities", "varchar(600) NULL");
        await SchemaHelpers.AddColumnIfMissingAsync(db, "room_allotments", "HasCapacityOverride", "tinyint(1) NOT NULL DEFAULT 1");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_rooms (
                HotelId varchar(32) NOT NULL,
                Id varchar(32) NOT NULL,
                Name varchar(100) NOT NULL,
                Price decimal(12,2) NOT NULL,
                Capacity int NOT NULL,
                SizeSquareMeters int NULL,
                BedType varchar(100) NULL,
                Features varchar(600) NULL,
                IsActive tinyint(1) NOT NULL,
                PRIMARY KEY (HotelId, Id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
        await SchemaHelpers.AddColumnIfMissingAsync(db, "hotel_rooms", "BedCount", "int NULL");
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS hotel_gallery_photos (
                Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                HotelId varchar(32) NOT NULL,
                RoomId varchar(32) NULL,
                Url varchar(1000) NOT NULL,
                Credit varchar(200) NULL,
                INDEX IX_hotel_gallery_photos_HotelId_RoomId (HotelId, RoomId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            """);
    }
}
