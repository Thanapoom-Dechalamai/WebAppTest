using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;
namespace Nontiwat_7362_L002.Services;

public class ReviewService(ApplicationDbContext db)
{
    public const int PageSize = 10;

    public async Task<ReviewSection> SectionAsync(string hotelId, ReviewQuery query)
    {
        var all = db.HotelReviews.AsNoTracking().Where(x => x.HotelId == hotelId && !x.IsSample);
        var stats = await all.GroupBy(x => x.Rating).Select(g => new { Rating = g.Key, Count = g.Count() }).ToListAsync();
        var total = stats.Sum(x => x.Count);
        var aspects = new Dictionary<string, double>();
        if (total > 0)
        {
            aspects["Cleanliness"] = await all.AverageAsync(x => (double)x.Cleanliness);
            aspects["Location"] = await all.AverageAsync(x => (double)x.Location);
            aspects["Service"] = await all.AverageAsync(x => (double)x.Service);
            aspects["Value"] = await all.AverageAsync(x => (double)x.Value);
        }
        var typeCounts = await all.GroupBy(x => x.TravelerType).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);

        var filtered = all;
        if (query.Stars is >= 1 and <= 5) filtered = filtered.Where(x => x.Rating == query.Stars);
        if (!string.IsNullOrEmpty(query.Type) && ReviewOptions.TravelerTypes.ContainsKey(query.Type)) filtered = filtered.Where(x => x.TravelerType == query.Type);
        filtered = query.Sort switch
        {
            "high" => filtered.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAtUtc),
            "low" => filtered.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAtUtc),
            _ => filtered.OrderByDescending(x => x.CreatedAtUtc)
        };
        var matchCount = await filtered.CountAsync();
        var pageCount = Math.Max(1, (int)Math.Ceiling(matchCount / (double)PageSize));
        var page = Math.Clamp(query.Page, 1, pageCount);
        var items = await filtered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        return new ReviewSection(total, total == 0 ? 0 : stats.Sum(x => x.Rating * x.Count) / (double)total,
            Enumerable.Range(1, 5).ToDictionary(i => i, i => stats.Where(x => x.Rating == i).Sum(x => x.Count)),
            aspects, typeCounts, items, matchCount, page, pageCount, query);
    }

    // A customer may review a stay once, after it is completed.
    public async Task<StayOrder?> ReviewableOrderAsync(int userId, int orderId) =>
        await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId
            && x.Status == BookingStatus.Completed && !db.HotelReviews.Any(r => r.OrderId == x.Id));

    public async Task<int> CreateAsync(int userId, ReviewForm form)
    {
        var order = await ReviewableOrderAsync(userId, form.OrderId)
            ?? throw new ArgumentException("รีวิวได้เฉพาะการเข้าพักที่เสร็จสิ้นแล้ว และรีวิวได้ครั้งเดียวต่อการจอง");
        if (!ReviewOptions.TravelerTypes.ContainsKey(form.TravelerType)) throw new ArgumentException("เลือกประเภทผู้เข้าพัก");
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
        var review = new HotelReview
        {
            HotelId = order.HotelId, OrderId = order.Id, UserId = userId,
            ReviewerName = $"{user.FirstName} {(user.LastName.Length > 0 ? user.LastName[..1] + "." : "")}".Trim(),
            ReviewerCountry = "ไทย", TravelerType = form.TravelerType,
            Rating = form.Rating, Cleanliness = form.Cleanliness, Location = form.Location, Service = form.Service, Value = form.Value,
            Title = form.Title.Trim(), Body = form.Body.Trim(), RoomName = order.RoomName, StayedOn = order.CheckIn, IsSample = false
        };
        db.HotelReviews.Add(review);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is MySqlConnector.MySqlException { Number: 1062 })
        { throw new ArgumentException("การจองนี้รีวิวไปแล้ว"); }
        return review.Id;
    }
}
