using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// The EF implementation of <see cref="IBookingStore"/>: plain reads for quoting, and <c>FOR UPDATE</c> row locks
/// for the actions that decide a booking. The lock order the service follows is wallet, then promotion/hotel/room,
/// then the hotel team; this adapter only ever locks the rows it is asked to.
/// </summary>
public sealed class EfBookingStore(ApplicationDbContext db, HotelRoomCatalog rooms) : IBookingStore
{
    public async Task<HotelData?> GetHotelForQuoteAsync(string hotelId) =>
        await db.Hotels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == hotelId && x.IsActive);

    public async Task<RoomOption?> GetRoomForQuoteAsync(string hotelId, string roomType) =>
        await rooms.FindAsync(hotelId, roomType) is { IsActive: true } room ? room : null;

    public async Task<HotelData> LockHotelForBookingAsync(string hotelId) =>
        (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {hotelId} FOR UPDATE").AsNoTracking().ToListAsync()).Single();

    public Task<RoomOption?> ReadRoomForBookingAsync(string hotelId, string roomType) =>
        rooms.FindAsync(hotelId, roomType);

    public Task<Promotion?> GetPromotionAsync(string code) =>
        db.Promotions.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code);

    public async Task<Promotion?> LockPromotionAsync(string code)
    {
        var rows = await db.Promotions.FromSqlInterpolated($"SELECT * FROM promotions WHERE Code={code} FOR UPDATE")
            .AsNoTracking().ToListAsync();
        return rows.SingleOrDefault();
    }

    public Task<bool> CustomerUsedPromotionAsync(int userId, int promotionId) =>
        db.PromotionRedemptions.AnyAsync(x => x.PromotionId == promotionId && x.UserId == userId);

    public Task<StayOrder?> FindOrderAsync(int userId, string requestId) =>
        db.Orders.SingleOrDefaultAsync(x => x.UserId == userId && x.RequestId == requestId);

    public async Task<SupportCase?> LockSupportCaseAsync(int caseId) =>
        (await db.SupportCases.FromSqlInterpolated($"SELECT * FROM support_cases WHERE Id = {caseId} FOR UPDATE").ToListAsync())
        .SingleOrDefault();

    public Task<SupportQuote?> PeekQuoteAsync(int quoteId) =>
        db.SupportQuotes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == quoteId);

    public async Task<SupportQuote?> LockQuoteAsync(int quoteId) =>
        (await db.SupportQuotes.FromSqlInterpolated($"SELECT * FROM support_quotes WHERE Id = {quoteId} FOR UPDATE").ToListAsync())
        .SingleOrDefault();

    public Task<int> OpenTransferCountAsync(int userId) =>
        db.Orders.CountAsync(x => x.UserId == userId && x.Status == BookingStatus.PaymentReview && x.PaymentMethod == PaymentMethods.Transfer);

    public Task<StayOrder?> PeekOrderAsync(int orderId) =>
        db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId);

    public Task<StayOrder?> PeekOrderForUserAsync(int orderId, int userId) =>
        db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);

    public Task<StayOrder> LoadOrderAsync(int orderId) =>
        db.Orders.SingleAsync(x => x.Id == orderId);

    public Task<StayOrder?> LoadOrderForUserAsync(int orderId, int userId) =>
        db.Orders.SingleOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);

    public async Task<IReadOnlyList<(int Id, int UserId)>> StaleUnconfirmedAsync(DateTime today, int take)
    {
        var stale = await db.Orders.AsNoTracking()
            .Where(x => (x.Status == BookingStatus.Pending || x.Status == BookingStatus.PaymentReview) && x.CheckIn <= today)
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.UserId }).Take(take).ToListAsync();
        return stale.Select(x => (x.Id, x.UserId)).ToList();
    }

    // Same rules as BookingLifecycle.OverdueStep, as SQL.
    public async Task<IReadOnlyList<(int Id, int UserId)>> OverdueStaysAsync(DateTime today, int take)
    {
        var due = await db.Orders.AsNoTracking()
            .Where(x => (x.Status == BookingStatus.Confirmed && x.CheckOut <= today) || (x.Status == BookingStatus.CheckedIn && x.CheckOut < today))
            .OrderBy(x => x.Id).Select(x => new { x.Id, x.UserId }).Take(take).ToListAsync();
        return due.Select(x => (x.Id, x.UserId)).ToList();
    }

    public Task<PromotionRedemption?> FindRedemptionAsync(int orderId) =>
        db.PromotionRedemptions.SingleOrDefaultAsync(x => x.OrderId == orderId);

    public Task RemoveRedemptionAsync(PromotionRedemption redemption)
    {
        db.PromotionRedemptions.Remove(redemption);
        return Task.CompletedTask;
    }

    public Task AddOrderAsync(StayOrder order)
    {
        db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task AddRedemptionAsync(PromotionRedemption redemption)
    {
        db.PromotionRedemptions.Add(redemption);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<int>> LockHotelTeamAsync(string hotelId)
    {
        var team = await db.HotelAssignments.FromSqlInterpolated(
            $"SELECT * FROM hotel_assignments WHERE HotelId = {hotelId} FOR UPDATE").AsNoTracking().ToListAsync();
        return team.Select(x => x.OwnerUserId).ToList();
    }
}
