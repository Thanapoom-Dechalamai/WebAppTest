using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services.Booking;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// The EF implementation of <see cref="IHotelLedger"/>: the hotel's receipt and the refund line that reverses it.
/// Both lines are recorded against the same team member, so a hotel's total always adds up.
/// </summary>
public sealed class EfHotelLedger(ApplicationDbContext db) : IHotelLedger
{
    public Task<bool> HasReceiptAsync(int orderId) =>
        db.HotelOwnerEntries.AnyAsync(x => x.OrderId == orderId && x.Kind == "Receipt");

    public Task<HotelOwnerEntry?> FindReceiptAsync(int orderId) =>
        db.HotelOwnerEntries.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId && x.Kind == "Receipt");

    public Task AddReceiptAsync(StayOrder order, int recorderUserId, decimal amount)
    {
        db.HotelOwnerEntries.Add(new HotelOwnerEntry { OwnerUserId = recorderUserId, OrderId = order.Id,
            HotelId = order.HotelId, HotelName = order.HotelName, Kind = "Receipt", Amount = amount });
        return Task.CompletedTask;
    }

    public Task AddReceiptReversalAsync(HotelOwnerEntry receipt)
    {
        db.HotelOwnerEntries.Add(new HotelOwnerEntry { OwnerUserId = receipt.OwnerUserId, OrderId = receipt.OrderId,
            HotelId = receipt.HotelId, HotelName = receipt.HotelName, Kind = "Refund", Amount = -receipt.Amount });
        return Task.CompletedTask;
    }
}
