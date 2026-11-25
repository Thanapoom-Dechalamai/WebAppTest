using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// Reading and locking the rows a booking touches: hotels, room types, promotions, orders and the hotel team.
/// The "Lock..." methods take a MySQL row lock (FOR UPDATE) so two people cannot book the last room or the
/// last use of a code at the same time; "Peek..." reads without keeping the row and "Load..." gives the tracked
/// row the changes are made on. The real implementation is Data/Booking/EfBookingStore; tests use fakes.
/// </summary>
public interface IBookingStore
{
    /// <summary>The hotel a booking quotes for, open for bookings; null when it is not there or is closed.</summary>
    Task<HotelData?> GetHotelForQuoteAsync(string hotelId);
    /// <summary>The room type a booking quotes for, open for bookings; null when it is not there or is closed.</summary>
    Task<RoomOption?> GetRoomForQuoteAsync(string hotelId, string roomType);

    /// <summary>Lock the hotel row so no booking lands while this one is decided (called only after the quote found it).</summary>
    Task<HotelData> LockHotelForBookingAsync(string hotelId);
    /// <summary>Read the current offer after the hotel row is locked by the caller.</summary>
    Task<RoomOption?> ReadRoomForBookingAsync(string hotelId, string roomType);

    /// <summary>The code a booking wants to use, or null when it does not exist.</summary>
    Task<Promotion?> GetPromotionAsync(string code);
    /// <summary>The same, with the row locked, so the last redemption cannot be taken twice at once.</summary>
    Task<Promotion?> LockPromotionAsync(string code);
    /// <summary>Has this customer already used this promotion (codes are one use per account)?</summary>
    Task<bool> CustomerUsedPromotionAsync(int userId, int promotionId);

    /// <summary>The customer's order for this request, when it already exists (a resend must not book twice).</summary>
    Task<StayOrder?> FindOrderAsync(int userId, string requestId);
    Task<SupportCase?> LockSupportCaseAsync(int caseId);

    /// <summary>A quote without a lock (to build the booking form), or null.</summary>
    Task<SupportQuote?> PeekQuoteAsync(int quoteId);

    /// <summary>A quote locked with FOR UPDATE inside the booking transaction, or null.</summary>
    Task<SupportQuote?> LockQuoteAsync(int quoteId);
    /// <summary>How many unchecked transfers this customer is still waiting on.</summary>
    Task<int> OpenTransferCountAsync(int userId);

    /// <summary>A quick look at an order by id, without keeping the row; null when it is not there.</summary>
    Task<StayOrder?> PeekOrderAsync(int orderId);
    /// <summary>The tracked order the changes are made on, read after the rows are locked.</summary>
    Task<StayOrder> LoadOrderAsync(int orderId);
    /// <summary>The customer's own order, tracked; null when it is not theirs.</summary>
    Task<StayOrder?> LoadOrderForUserAsync(int orderId, int userId);

    /// <summary>Requests nobody confirmed before their check-in day: closed in batches so one slow order cannot stall the rest.</summary>
    Task<IReadOnlyList<(int Id, int UserId)>> StaleUnconfirmedAsync(DateTime today, int take);

    /// <summary>Confirmed stays whose check-out day has come, and checked-in stays whose check-out day has passed.</summary>
    Task<IReadOnlyList<(int Id, int UserId)>> OverdueStaysAsync(DateTime today, int take);

    /// <summary>Which promotion redemption belongs to this booking, if it was booked with one.</summary>
    Task<PromotionRedemption?> FindRedemptionAsync(int orderId);
    Task RemoveRedemptionAsync(PromotionRedemption redemption);

    Task AddOrderAsync(StayOrder order);
    Task AddRedemptionAsync(PromotionRedemption redemption);

    /// <summary>
    /// The hotel's member ids, locked with the team's assignment rows.
    /// Lock order for every money-sensitive action: the customer's wallet, then these rows.
    /// </summary>
    Task<IReadOnlyList<int>> LockHotelTeamAsync(string hotelId);
}
