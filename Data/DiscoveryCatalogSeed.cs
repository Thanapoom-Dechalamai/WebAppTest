using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.Data;
public static class DiscoveryCatalogSeed
{
 public record Fact(string Id,string Category,string ViewType,bool? HasPool,string SourceUrl,string Description,decimal? SeaDistanceKm=null);
 public record Tag(string Id,string Theme,string SourceUrl);
 public record Seed(List<Fact> Facts,List<Tag> Tags);
 public static async Task EnsureAsync(ApplicationDbContext db,string root)
 {
  await db.Database.ExecuteSqlRawAsync("""
   CREATE TABLE IF NOT EXISTS hotel_theme_tags (
    HotelId varchar(32) NOT NULL, Theme varchar(24) NOT NULL,
    SourceUrl varchar(1000) NOT NULL, VerifiedAtUtc datetime(6) NOT NULL,
    PRIMARY KEY(HotelId,Theme), FOREIGN KEY(HotelId) REFERENCES hotels(Id) ON DELETE CASCADE
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
   """);
  var data=JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(Path.Combine(root,"Database","discovery-catalog.json")))!;
  await using var tx=await db.Database.BeginTransactionAsync();
  if(await db.Database.ExecuteSqlRawAsync("INSERT IGNORE INTO catalog_versions(Id) VALUES(3)")==0) {await tx.CommitAsync();return;}
  foreach(var fact in data.Facts) {
   if(!StayEnvironmentOptions.Categories.ContainsKey(fact.Category) ||
      (fact.ViewType!="" && !StayEnvironmentOptions.Views.ContainsKey(fact.ViewType)))
       throw new InvalidOperationException("Unsupported discovery category");
   if(!await db.Hotels.AnyAsync(x=>x.Id==fact.Id)) continue;
   var row=await db.StayEnvironments.SingleOrDefaultAsync(x=>x.HotelId==fact.Id);
   if(row is not null && (row.Category!="" || row.ViewType!="" || row.HasPool is not null || row.SourceUrl is not null)) continue;
   if(row is null){row=new StayEnvironment{HotelId=fact.Id};db.StayEnvironments.Add(row);}
   row.Category=fact.Category;row.ViewType=fact.ViewType;row.HasPool=fact.HasPool;
   row.SourceUrl=fact.SourceUrl;row.ViewDescription=fact.Description;row.SeaDistanceKm=fact.SeaDistanceKm;
   row.VerifiedAtUtc=DateTime.UtcNow;row.UpdatedAtUtc=DateTime.UtcNow;
  }
  foreach(var tag in data.Tags) {
   if(!StayEnvironmentOptions.DiscoveryCategories.ContainsKey(tag.Theme)) throw new InvalidOperationException("Unsupported discovery theme");
   if(!await db.Hotels.AnyAsync(x=>x.Id==tag.Id)) continue;
   if(!await db.HotelThemeTags.AnyAsync(x=>x.HotelId==tag.Id&&x.Theme==tag.Theme))
    db.HotelThemeTags.Add(new(){HotelId=tag.Id,Theme=tag.Theme,SourceUrl=tag.SourceUrl,VerifiedAtUtc=DateTime.UtcNow});
  }
  await db.SaveChangesAsync();
  await tx.CommitAsync();
 }
}
