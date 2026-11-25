using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Services;
public class CatalogService(ApplicationDbContext db, HotelRoomCatalog roomCatalog)
{
 public async Task<IReadOnlyList<HotelCardViewModel>> AllAsync(bool includeInactive = false)
 {
  var themes = (await db.HotelThemeTags.AsNoTracking().ToListAsync()).ToLookup(x=>x.HotelId,x=>x.Theme);
  var facts = await db.StayEnvironments.AsNoTracking().ToDictionaryAsync(x=>x.HotelId);
  var reviews = await db.HotelReviews.AsNoTracking().Where(x=>!x.IsSample).GroupBy(x=>x.HotelId)
   .Select(g=>new{HotelId=g.Key,Score=g.Average(x=>(double)x.Rating),Count=g.Count()}).ToDictionaryAsync(x=>x.HotelId);
  var hotels = await db.Hotels.AsNoTracking().Where(x=>includeInactive || x.IsActive).OrderByDescending(x=>x.IsActive)
    .ThenByDescending(x=>x.PhotoUrl!=null && x.PhotoUrl!="").ThenBy(x=>x.Id).ToListAsync();
  var startingRates = await roomCatalog.StartingRatesAsync(hotels);
  return hotels.Select(h=>new HotelCardViewModel(h.Id,h.Name,h.City,h.ImageKey,h.SourceUrl,h.Price)
    {Themes=themes[h.Id].ToList(),PhotoUrl=string.IsNullOrEmpty(h.PhotoUrl)?null:h.PhotoUrl,PhotoSourceUrl=h.PhotoSourceUrl,PhotoCredit=h.PhotoCredit,Environment=facts.GetValueOrDefault(h.Id) ?? new StayEnvironment {HotelId=h.Id},
    StartingRate=startingRates[h.Id],PublicDescription=h.Description,Amenities=h.Amenities?.Split('\n',StringSplitOptions.RemoveEmptyEntries),
    ReviewScore=reviews.TryGetValue(h.Id,out var r)?Math.Round(r.Score,1):null,ReviewCount=reviews.TryGetValue(h.Id,out var c)?c.Count:0}).ToList();
 }
 public async Task<HotelCardViewModel?> FindAsync(string id) => (await AllAsync()).SingleOrDefault(x=>x.Id==id);
 public Task<IReadOnlyList<RoomOption>> RoomsAsync(string hotelId, bool includeInactive = false) =>
  roomCatalog.ListAsync(hotelId, includeInactive);
 public async Task<IReadOnlyList<RoomOption>> RoomsAsync() => (await db.RoomTypes.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Extra).ToListAsync())
  .Select(x=>new RoomOption(x.Id,x.Name,x.Extra,x.Image,x.Features)).ToList();
}
