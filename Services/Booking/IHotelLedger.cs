using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// The hotel side of the money: the receipt a hotel earns and the refund line that reverses it.
/// Revenue belongs to the hotel (see <c>FinanceRules</c>), not to Lodgo's accounts.
/// The real implementation is <c>Data/Booking/EfHotelLedger</c>; tests use fakes.
/// </summary>
public interface IHotelLedger
{
    /// <summary>Has the hotel already earned its receipt for this booking?</summary>
    Task<bool> HasReceiptAsync(int orderId);
    /// <summary>The receipt line, when one has been written.</summary>
    Task<HotelOwnerEntry?> FindReceiptAsync(int orderId);

    /// <summary>The hotel earns its share: a receipt of the booking's total, recorded by the acting team member.</summary>
    Task AddReceiptAsync(StayOrder order, int recorderUserId, decimal amount);
    /// <summary>Reverse the hotel's receipt with a same-amount refund line, under the original recorder.</summary>
    Task AddReceiptReversalAsync(HotelOwnerEntry receipt);
}
