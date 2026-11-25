using Nontiwat_7362_L002.Services.Booking;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Support;

/// <summary>Connects the quote workflow to the real checkout: same price rules, availability check and booking code.</summary>
public sealed class CheckoutQuoteAdapter(CheckoutService checkout, IBookingStore store) : IQuoteCheckout
{
    public async Task<QuotePrice> PriceAsync(int customerId, BookingForm form)
    {
        var (subtotal, room) = await checkout.QuoteDetailsAsync(form);
        var offer = await checkout.OfferAsync(customerId, form, subtotal);
        var hotel = await store.GetHotelForQuoteAsync(form.HotelId) ?? throw new ArgumentException("ไม่พบที่พักนี้");
        return new QuotePrice(hotel.Name, room.Name, subtotal, offer.Discount, offer.Total, offer.Code);
    }

    public Task<int> BookAsync(int customerId, int quoteId, QuotePayment payment) =>
        checkout.BookFromQuoteAsync(customerId, quoteId, payment);
}
