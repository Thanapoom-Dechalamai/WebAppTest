using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services.Booking;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// The EF implementation of <see cref="IBookingJournal"/>: history lines, the single decision row and the
/// notifications. Text is cut to the column limits here as the last line of defence; the caller already
/// passes it cut, so this only matters when a caller forgets.
/// </summary>
public sealed class EfBookingJournal(ApplicationDbContext db) : IBookingJournal
{
    private const int NoteMax = 255;
    private const int TitleMax = 120;
    private const int BodyMax = 255;

    public Task AddEventAsync(StayOrder order, string status, string note, int? actorUserId, string actorLabel)
    {
        db.BookingEvents.Add(new BookingEvent { OrderId = order.Id, Status = status, Note = BookingText.Cut(note, NoteMax),
            ActorUserId = actorUserId, ActorLabel = BookingText.Cut(actorLabel, TitleMax) });
        return Task.CompletedTask;
    }

    public async Task RecordDecisionAsync(StayOrder order, int actorId, string action, string note, DateTime nowUtc)
    {
        var decision = await db.HotelDecisions.SingleOrDefaultAsync(x => x.OrderId == order.Id);
        if (decision is null) { decision = new HotelDecision { OrderId = order.Id }; db.HotelDecisions.Add(decision); }
        decision.ActorUserId = actorId;
        decision.Action = action;
        decision.Note = BookingText.Cut(note, NoteMax);
        decision.UpdatedAtUtc = nowUtc;
    }

    public Task NotifyCustomerAsync(StayOrder order, string title, string body)
    {
        db.Notifications.Add(new Notification { UserId = order.UserId, OrderId = order.Id,
            Title = BookingText.Cut(title, TitleMax), Body = BookingText.Cut(body, BodyMax),
            Url = "/Stays/Order/" + order.Id });
        return Task.CompletedTask;
    }

    public async Task NotifyHotelTeamAsync(StayOrder order, string title, string body)
    {
        var team = await (from a in db.HotelAssignments.AsNoTracking()
                          join u in db.Users.AsNoTracking() on a.OwnerUserId equals u.Id
                          where a.HotelId == order.HotelId && u.Role == AppRoles.HotelOwner
                          select u.Id).Distinct().ToListAsync();
        foreach (var memberId in team)
            db.Notifications.Add(new Notification { UserId = memberId, OrderId = order.Id, Title = BookingText.Cut(title, TitleMax),
                Body = BookingText.Cut(order.HotelName + " · " + body, BodyMax), Url = "/HotelOwner/Index?status=" + order.Status });
    }

    public async Task NotifyAccountsAsync(StayOrder order, string title, string body)
    {
        var accounts = await db.Users.AsNoTracking()
            .Where(x => x.Role == AppRoles.AC && !x.IsSuspended).Select(x => x.Id).ToListAsync();
        foreach (var id in accounts)
            db.Notifications.Add(new Notification { UserId = id, OrderId = order.Id, Title = BookingText.Cut(title, TitleMax),
                Body = BookingText.Cut(body, BodyMax), Url = "/AC/Payments" });
    }
}
