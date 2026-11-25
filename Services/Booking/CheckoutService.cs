using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services.Support;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class CheckoutService(
    IBookingUnitOfWork uow, IBookingStore store, IWalletStore walletStore, IBookingJournal journal,
    IActorDirectory actors, IBookingAvailability availability, IThaiClock clock)
{
    private DateTime Today => clock.Today;

    public async Task<decimal> QuoteAsync(BookingForm form) => (await QuoteDetailsAsync(form)).Subtotal;

    public async Task<(decimal Subtotal, RoomOption Room)> QuoteDetailsAsync(BookingForm form)
    {
        var hotel = await store.GetHotelForQuoteAsync(form.HotelId) ?? throw new ArgumentException("ไม่พบที่พักนี้");
        var room = await store.GetRoomForQuoteAsync(hotel.Id, form.RoomType) ?? throw new ArgumentException("ไม่พบประเภทห้อง");
        if (!BookingRules.StayDatesValid(form.CheckIn, form.CheckOut, Today)) throw new ArgumentException(BookingRules.StayDatesRule);
        if (!BookingRules.PartyValid(form.Rooms, form.Guests)) throw new ArgumentException(BookingRules.PartyRule);
        if (!PaymentRules.RequestIdValid(form.RequestId)) throw new ArgumentException(PaymentRules.BadRequestIdRule);
        var nightlyRate = room.NightlyRate(hotel.Price);
        if (!BookingPricing.RatesValid(room.HotelPrice ?? hotel.Price, room.Extra)) throw new ArgumentException(BookingPricing.QuoteRateRule);
        await availability.EnsureAvailableAsync(hotel.Id, room.Id, room.Name, form.CheckIn, form.CheckOut, form.Rooms);
        return (BookingPricing.Subtotal(nightlyRate, 0, BookingPricing.Nights(form.CheckIn, form.CheckOut), form.Rooms), room);
    }

    public record Offer(decimal Total, decimal Discount, int? PromotionId, string? Code);

    public async Task<Offer> OfferAsync(int userId, BookingForm form, decimal subtotal, bool locked = false)
    {
        var code = BookingPricing.NormaliseCode(form.PromotionCode);
        if (code is null) return new(subtotal, 0, null, null);
        var promotion = locked ? await store.LockPromotionAsync(code) : await store.GetPromotionAsync(code);
        if (BookingPricing.CodeClosedReason(promotion, Today) is string closed) throw new ArgumentException(closed);
        if (await store.CustomerUsedPromotionAsync(userId, promotion!.Id))
            throw new ArgumentException("คุณใช้โค้ดนี้แล้ว โค้ดใช้ได้ครั้งเดียวต่อบัญชี (คืนสิทธิ์เฉพาะกรณีโรงแรมหรือระบบเป็นฝ่ายยกเลิก)");
        if (BookingPricing.CodeTooSmallReason(promotion!, BookingPricing.Nights(form.CheckIn, form.CheckOut), subtotal) is string small)
            throw new ArgumentException(small);
        var discount = BookingPricing.Discount(promotion!, subtotal);
        if (!discount.Ok) throw new ArgumentException(discount.Error);
        return new(subtotal - discount.Amount, discount.Amount, promotion!.Id, promotion.Code);
    }

    public Task<int> BookAsync(int userId, BookingForm form) => BookCoreAsync(userId, form, userId, null);

    // The customer accepts a quote support sent in a ticket (2026-11-25): the quoted stay at the quoted total, paid the
    // way the customer chose. Support can no longer book and charge a customer directly.
    public async Task<int> BookFromQuoteAsync(int customerId, int quoteId, QuotePayment payment)
    {
        var quote = await store.PeekQuoteAsync(quoteId);
        if (quote is null || quote.CustomerId != customerId) throw new KeyNotFoundException();
        return await BookCoreAsync(customerId, QuoteRules.ToBookingForm(quote, payment), customerId, quote.CaseId, quoteId);
    }

    private async Task<int> BookCoreAsync(int userId, BookingForm form, int actorId, int? caseId, int? quoteId = null)
    {
        await walletStore.EnsureWalletAsync(userId);
        await uow.BeginAsync();
        try
        {
            var wallet = await walletStore.LockWalletAsync(userId);
            SupportCase? item = null;
            if (caseId is int supportId)
            {
                item = await store.LockSupportCaseAsync(supportId);
                if (item is null || item.CustomerId != userId || item.Status == SupportStatus.Closed)
                    throw new ArgumentException("เรื่องนี้ปิดแล้วหรือไม่สามารถทำรายการแทนลูกค้าได้");
            }
            var old = await store.FindOrderAsync(userId, form.RequestId);
            if (old is not null)
            {
                if (item is not null && item.OrderId != old.Id) throw new ArgumentException("เลขคำขอถูกใช้แล้ว กรุณาเริ่มรายการใหม่");
                await uow.CommitAsync();
                return old.Id;
            }
            if (item?.OrderId is int linkedId && await store.PeekOrderAsync(linkedId) is { Status: not BookingStatus.Cancelled })
                throw new ArgumentException("แชตนี้มีการจองอยู่แล้ว ยกเลิกตามเงื่อนไขก่อนสร้างรายการใหม่");
            SupportQuote? quote = null;
            if (quoteId is int offerId)
            {
                // Locked after the wallet and the ticket, before the hotel (lock order wallet → ticket → quote → hotel).
                quote = await store.LockQuoteAsync(offerId);
                if (quote is null || quote.CustomerId != userId || quote.CaseId != caseId) throw new KeyNotFoundException();
                if (QuoteRules.DecisionError(quote, clock.UtcNow) is string closedQuote) throw new ArgumentException(closedQuote);
            }
            await QuoteAsync(form);
            var hotel = await store.LockHotelForBookingAsync(form.HotelId);
            var room = await store.ReadRoomForBookingAsync(hotel.Id, form.RoomType);
            if (!hotel.IsActive || room is not { IsActive: true }) throw new ArgumentException("ที่พักหรือประเภทห้องนี้ปิดให้จองแล้ว");
            // Checked again under the hotel row lock: two people cannot both take the last room.
            await availability.EnsureAvailableAsync(hotel.Id, room.Id, room.Name, form.CheckIn, form.CheckOut, form.Rooms);
            var nightlyRate = room.NightlyRate(hotel.Price);
            var subtotal = BookingPricing.Subtotal(nightlyRate, 0, BookingPricing.Nights(form.CheckIn, form.CheckOut), form.Rooms);
            if (!BookingPricing.RatesValid(room.HotelPrice ?? hotel.Price, room.Extra)) throw new ArgumentException(BookingPricing.BookingRateRule);
            var offer = await OfferAsync(userId, form, subtotal, true);
            var total = offer.Total;
            if (!BookingPricing.TotalUnchanged(form.ExpectedTotal, total)) throw new ArgumentException(BookingPricing.TotalChangedRule);
            var transfer = PaymentRules.IsTransfer(form.PaymentMethod);
            if (transfer && !PaymentRules.TransferEvidenceGiven(form.PaymentRef, form.SlipFile))
                throw new ArgumentException(PaymentRules.TransferEvidenceRule);
            // Unchecked transfers hold rooms, so one customer may keep only a few waiting at once.
            if (transfer && !PaymentRules.MayOpenAnotherTransfer(await store.OpenTransferCountAsync(userId)))
                throw new ArgumentException(PaymentRules.TooManyOpenTransfersRule);
            if (!transfer && !PaymentRules.CreditCovers(wallet.Balance, total)) throw new ArgumentException(PaymentRules.NotEnoughCreditRule);
            var order = new StayOrder { UserId = userId, RequestId = form.RequestId, HotelId = hotel.Id,
                HotelName = hotel.Name, City = hotel.City, RoomName = room.Name, RoomType = room.Id, CheckIn = form.CheckIn.Date,
                CheckOut = form.CheckOut.Date, Rooms = form.Rooms, Guests = form.Guests,
                NightlyRate = nightlyRate, Subtotal = subtotal, DiscountAmount = offer.Discount, PromotionCode = offer.Code,
                Total = total, SpecialRequest = BookingRules.CleanRequest(form.SpecialRequest), CommissionPercent = hotel.CommissionPercent,
                PaymentMethod = PaymentRules.MethodOf(form.PaymentMethod), Status = BookingStatus.PaymentReview,
                PaymentRef = transfer ? PaymentRules.CleanPaymentRef(form.PaymentRef) : null,
                SlipFile = transfer ? form.SlipFile : null, HotelSentAtUtc = null };
            await store.AddOrderAsync(order);
            // Credit is taken at booking time, before accounts check it; a refusal refunds it.
            if (!transfer)
                await walletStore.AddEntryAsync(wallet, "order:" + form.RequestId, "Payment", -total,
                    PaymentRules.CreditHeldDescription(hotel.Name));
            await uow.SaveAsync();
            if (offer.PromotionId is int promoId)
            {
                await store.AddRedemptionAsync(new PromotionRedemption { PromotionId = promoId, UserId = userId, OrderId = order.Id,
                    Code = offer.Code!, Subtotal = subtotal, DiscountAmount = offer.Discount });
                await uow.SaveAsync();
            }
            if (item is not null)
            {
                item.OrderId = order.Id;
                item.UpdatedAtUtc = clock.UtcNow;
            }
            if (quote is not null)
            {
                quote.Status = QuoteStatus.Accepted;
                quote.OrderId = order.Id;
                quote.DecidedAtUtc = clock.UtcNow;
            }
            await PaymentWaitingAsync(order, actorId, quote is null ? null : QuoteRules.Number(quote.Id));
            await uow.SaveAsync();
            await uow.CommitAsync();
            return order.Id;
        }
        catch { await uow.RollbackAsync(); throw; }
    }

    private async Task PaymentWaitingAsync(StayOrder order, int actorId, string? quoteNumber)
    {
        var actor = await actors.DescribeAsync(actorId);
        var transfer = PaymentRules.IsTransfer(order.PaymentMethod);
        var note = (quoteNumber is null ? "" : $"ลูกค้ายืนยันใบเสนอราคา {quoteNumber} · ") + PaymentRules.WaitingNote(transfer);
        await journal.AddEventAsync(order, BookingStatus.PaymentReview, note, actorId, actor.Label);
        await journal.NotifyAccountsAsync(order, transfer ? "เงินโอนรอตรวจ" : "การจองรอตรวจการชำระเงิน",
            $"{BookingText.Number(order.Id)} · {order.HotelName} · ฿{order.Total:N2}");
    }
}
