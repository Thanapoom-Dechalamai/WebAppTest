using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels.Roles;

namespace Nontiwat_7362_L002.Services;

// The hotel's public reply under a review. Only the hotel's own team (or Admin) may reply.
public class ReviewReplyService(ApplicationDbContext db, CatalogService catalog)
{
    public const int MaxLength = 1000;

    private IQueryable<string> TeamHotels(int actorId) =>
        db.HotelAssignments.Where(x => x.OwnerUserId == actorId).Select(x => x.HotelId);

    public async Task<ReviewReplyPage> ListAsync(int actorId, bool isAdmin, string? hotelId, bool unansweredOnly)
    {
        var allowed = isAdmin ? null : (await TeamHotels(actorId).Distinct().ToListAsync()).ToHashSet();
        var hotels = (await catalog.AllAsync()).Where(h => allowed is null || allowed.Contains(h.Id)).ToList();
        var hotel = hotels.FirstOrDefault(h => h.Id == hotelId) ?? (hotelId is null ? hotels.FirstOrDefault() : null);
        if (hotelId is not null && hotel is null) throw new UnauthorizedAccessException();
        if (hotel is null) return new(hotels, null, unansweredOnly ? ReviewReplyPage.Unanswered : ReviewReplyPage.All, [], 0, 0);
        var reviews = db.HotelReviews.AsNoTracking().Where(x => x.HotelId == hotel.Id && !x.IsSample);
        var shown = unansweredOnly ? reviews.Where(x => x.ReplyBody == null) : reviews;
        var items = await shown.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Take(ReviewReplyPage.PageSize).ToListAsync();
        return new(hotels, hotel, unansweredOnly ? ReviewReplyPage.Unanswered : ReviewReplyPage.All, items,
            await reviews.CountAsync(x => x.ReplyBody == null),
            await reviews.CountAsync());
    }

    // Reply or edit the reply. Throws UnauthorizedAccessException (not the team), KeyNotFoundException, ArgumentException (text).
    public async Task SaveAsync(int actorId, bool isAdmin, int reviewId, string? body)
    {
        body = body?.Trim() ?? "";
        if (body.Length == 0) throw new ArgumentException("กรอกคำตอบก่อนบันทึก");
        if (body.Length > MaxLength) throw new ArgumentException("คำตอบยาวได้ไม่เกิน 1,000 ตัวอักษร");
        var review = await LoadForTeamAsync(actorId, isAdmin, reviewId);
        var first = review.ReplyBody is null;
        review.ReplyBody = body;
        review.ReplyByUserId = actorId;
        review.ReplyAtUtc = DateTime.UtcNow;
        if (first && review.UserId is int reviewer)
        {
            var hotel = await catalog.FindAsync(review.HotelId);
            db.Notifications.Add(new Notification { UserId = reviewer, OrderId = review.OrderId,
                Title = "ที่พักตอบรีวิวของคุณแล้ว",
                Body = $"{hotel?.Name ?? "ที่พัก"} ตอบรีวิว \"{Cut(review.Title, 80)}\"",
                Url = $"/Stays/Details/{review.HotelId}#review-{review.Id}" });
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int actorId, bool isAdmin, int reviewId)
    {
        var review = await LoadForTeamAsync(actorId, isAdmin, reviewId);
        review.ReplyBody = null; review.ReplyByUserId = null; review.ReplyAtUtc = null;
        await db.SaveChangesAsync();
    }

    // Real guest reviews waiting for a reply in the hotels of this team member (sample reviews are not counted).
    public Task<int> UnansweredCountAsync(int staffUserId) =>
        db.HotelReviews.CountAsync(x => x.ReplyBody == null && !x.IsSample && TeamHotels(staffUserId).Contains(x.HotelId));

    private async Task<HotelReview> LoadForTeamAsync(int actorId, bool isAdmin, int reviewId)
    {
        var review = await db.HotelReviews.SingleOrDefaultAsync(x => x.Id == reviewId && !x.IsSample) ?? throw new KeyNotFoundException();
        if (!isAdmin && !await TeamHotels(actorId).ContainsAsync(review.HotelId)) throw new UnauthorizedAccessException();
        return review;
    }

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
