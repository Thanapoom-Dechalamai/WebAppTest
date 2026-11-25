using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;
namespace Nontiwat_7362_L002.Data;
public static class VerifiedCatalogSeed
{
 public record Photo(string Id,string PhotoUrl,string PhotoSourceUrl,string PhotoCredit);
 public record Fact(string Id,string Category,string ViewType,bool? HasPool,string SourceUrl,string? PhotoUrl,string PhotoCredit,string Description,decimal? SeaDistanceKm=null);
 public static async Task EnsureAsync(ApplicationDbContext db,string root,DateTime today)
 {
  var facts=JsonSerializer.Deserialize<List<Fact>>(await File.ReadAllTextAsync(Path.Combine(root,"Database","verified-hotels.json")))!;
  await using var tx=await db.Database.BeginTransactionAsync();
  foreach(var f in facts) {
   if(!await db.Hotels.AnyAsync(x=>x.Id==f.Id)) continue;
   await db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT IGNORE INTO stay_environments(HotelId,Category,ViewType,HasPool,SeaDistanceKm,ViewDescription,SourceUrl,VerifiedAtUtc,UpdatedAtUtc)
    VALUES({f.Id},{f.Category},{f.ViewType},{f.HasPool},{f.SeaDistanceKm},{f.Description},{f.SourceUrl},{DateTime.UtcNow},{DateTime.UtcNow})
    """);
   await db.Database.ExecuteSqlInterpolatedAsync($"""
    UPDATE stay_environments SET Category={f.Category},ViewType={f.ViewType},HasPool={f.HasPool},SeaDistanceKm={f.SeaDistanceKm},ViewDescription={f.Description},SourceUrl={f.SourceUrl},VerifiedAtUtc={DateTime.UtcNow},UpdatedAtUtc={DateTime.UtcNow}
    WHERE HotelId={f.Id} AND Category='' AND ViewType='' AND HasPool IS NULL AND SourceUrl IS NULL
    """);
   if(f.PhotoUrl is not null)
    await db.Database.ExecuteSqlInterpolatedAsync($"""
     UPDATE hotels SET PhotoUrl={f.PhotoUrl},PhotoSourceUrl={f.SourceUrl},PhotoCredit={f.PhotoCredit}
     WHERE Id={f.Id} AND PhotoUrl IS NULL
     """);
  }
  // Photo mappings are independent of environment facts; never infer a view from a picture.
  var photos=JsonSerializer.Deserialize<List<Photo>>(await File.ReadAllTextAsync(Path.Combine(root,"Database","hotel-photos.json")))!;
  foreach(var photo in photos)
   await db.Database.ExecuteSqlInterpolatedAsync($"""
    UPDATE hotels SET PhotoUrl={photo.PhotoUrl},PhotoSourceUrl={photo.PhotoSourceUrl},PhotoCredit={photo.PhotoCredit}
    WHERE Id={photo.Id} AND PhotoUrl IS NULL
    """);
  var actor=await db.Users.Where(x=>x.Role=="Admin").Select(x=>(int?)x.Id).FirstOrDefaultAsync() ?? 0;
  foreach(var promo in new[]{
   new Promotion{Code="STAY15",Title="วันพักผ่อนที่นานขึ้น ลด 15%",Details="พักอย่างน้อย 2 คืน รับส่วนลด 15% สูงสุด 1,000 เครดิต",DiscountKind="Percent",DiscountValue=15,MinNights=2,MaxDiscount=1000},
   new Promotion{Code="GREEN300",Title="เริ่มทริปสีเขียว ลด 300",Details="ยอดจองก่อนลดตั้งแต่ 2,500 เครดิต รับส่วนลด 300 เครดิต",DiscountKind="Fixed",DiscountValue=300,MinSpend=2500},
   new Promotion{Code="SLOW500",Title="พักช้าลงอีกนิด ลด 500",Details="พักอย่างน้อย 3 คืน ยอดก่อนลดตั้งแต่ 5,000 เครดิต ลด 500 เครดิต",DiscountKind="Fixed",DiscountValue=500,MinNights=3,MinSpend=5000}
  }) {
   // Closed or expired codes stay closed; startup never renews an existing offer.
   if(await db.Promotions.AnyAsync(x=>x.Code==promo.Code)) continue;
   promo.StartsOn=today;promo.EndsOn=today.AddDays(90);promo.UpdatedByUserId=actor;
   db.Promotions.Add(promo);
  }
  await db.SaveChangesAsync();
  await tx.CommitAsync();
 }
}
