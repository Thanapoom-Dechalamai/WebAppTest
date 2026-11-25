using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// The booking's history and its in-site messages: one <see cref="BookingEvent"/> line per status change,
/// the single hotel-decision record, and the notifications that reach the customer and the hotel team.
/// Texts are passed in already cut to the column limits (<c>BookingText.Cut</c>); this port only writes rows.
/// The real implementation is <c>Data/Booking/EfBookingJournal</c>; tests use fakes.
/// </summary>
public interface IBookingJournal
{
    /// <summary>One line in the booking's history: who moved it to which status and what was said.</summary>
    Task AddEventAsync(StayOrder order, string status, string note, int? actorUserId, string actorLabel);

    /// <summary>The booking's decision row (created on first use): which action and by whom, and when.</summary>
    Task RecordDecisionAsync(StayOrder order, int actorId, string action, string note, DateTime nowUtc);

    /// <summary>A message to the customer under the bell, pointing at the booking.</summary>
    Task NotifyCustomerAsync(StayOrder order, string title, string body);

    /// <summary>
    /// A message to every member of the hotel's team: the title is sent as-is,
    /// the body is prefixed with the hotel's name so the message still reads in a feed of many hotels.
    /// </summary>
    Task NotifyHotelTeamAsync(StayOrder order, string title, string body);

    /// <summary>
    /// A message to every active accounts (AC) user, pointing at the payments queue.
    /// Suspended accounts are not messaged.
    /// </summary>
    Task NotifyAccountsAsync(StayOrder order, string title, string body);
}
