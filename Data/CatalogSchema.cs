using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Data;
public static class CatalogSchema
{
 public static async Task EnsureAsync(ApplicationDbContext db)
 {
  await db.Database.ExecuteSqlRawAsync("""
   CREATE TABLE IF NOT EXISTS hotels (
    Id varchar(32) PRIMARY KEY, Name varchar(160) NOT NULL, City varchar(80) NOT NULL,
    ImageKey varchar(80) NOT NULL, SourceUrl varchar(1000) NOT NULL, Price int NOT NULL, IsActive tinyint(1) NOT NULL DEFAULT 1
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
   """);
  await db.Database.ExecuteSqlRawAsync("""
   CREATE TABLE IF NOT EXISTS room_types (
    Id varchar(32) PRIMARY KEY, Name varchar(100) NOT NULL, Extra decimal(12,2) NOT NULL,
    Image varchar(80) NOT NULL, Features varchar(600) NOT NULL, IsActive tinyint(1) NOT NULL DEFAULT 1
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
   """);
  await SchemaHelpers.AddColumnIfMissingAsync(db, "hotels", "UseSharedRooms", "tinyint(1) NOT NULL DEFAULT 1");
  await db.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS catalog_versions (Id int PRIMARY KEY) ENGINE=InnoDB;");
  await using var tx = await db.Database.BeginTransactionAsync();
  var first = await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO catalog_versions (Id) VALUES (1)");
  if(first==1) {
   foreach(var h in HotelCatalog.All)
    await db.Database.ExecuteSqlInterpolatedAsync($"""
     INSERT IGNORE INTO hotels (Id,Name,City,ImageKey,SourceUrl,Price,IsActive)
     VALUES ({h.Id},{h.Name},{h.City},{h.ImageKey},{h.SourceUrl},{h.Price},1)
     """);
   foreach(var r in RoomOptions.All)
    await db.Database.ExecuteSqlInterpolatedAsync($"""
     INSERT IGNORE INTO room_types (Id,Name,Extra,Image,Features,IsActive)
     VALUES ({r.Id},{r.Name},{r.Extra},{r.Image},{r.Features},1)
     """);
  }
  await tx.CommitAsync();
 }
}
