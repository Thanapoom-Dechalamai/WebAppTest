using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Support;

/// <summary>What the quote workflow needs from the database (EF: <c>Data/Support/EfSupportQuoteStore</c>).</summary>
public interface ISupportQuoteStore
{
    /// <summary>The ticket locked with FOR UPDATE inside the open transaction, or null.</summary>
    Task<SupportCase?> LockCaseAsync(int caseId);

    /// <summary>True when the account is an active (not suspended) Customer.</summary>
    Task<bool> CustomerCanBookAsync(int customerId);

    /// <summary>True when the ticket is already linked to a booking that is not cancelled.</summary>
    Task<bool> HasActiveOrderAsync(SupportCase ticket);

    /// <summary>The ticket's quotes still marked Open (tracked, so changes are saved with the transaction).</summary>
    Task<IReadOnlyList<SupportQuote>> OpenQuotesAsync(int caseId);

    /// <summary>One quote locked with FOR UPDATE, or null.</summary>
    Task<SupportQuote?> LockQuoteAsync(int quoteId);

    /// <summary>One quote without a lock, or null.</summary>
    Task<SupportQuote?> FindQuoteAsync(int quoteId);

    void AddQuote(SupportQuote quote);
}

/// <summary>Prices and books through the real checkout (adapter: <see cref="CheckoutQuoteAdapter"/>).</summary>
public interface IQuoteCheckout
{
    /// <summary>Checks dates, party, room, availability and the code for this customer. Throws ArgumentException (Thai).</summary>
    Task<QuotePrice> PriceAsync(int customerId, BookingForm form);

    /// <summary>Books the quote for its customer in one transaction (locks wallet, ticket, quote, hotel). Returns the order id.</summary>
    Task<int> BookAsync(int customerId, int quoteId, QuotePayment payment);
}

/// <summary>Posts one line to a ticket's chat as support or as the customer (implemented by SupportService).</summary>
public interface ISupportChat
{
    Task PostAsync(int actorId, int caseId, string body, bool fromStaff);
}

/// <summary>
/// Quotes in a ticket (B1, 2026-11-25): support sends a price; the customer accepts it with a payment method, or declines it.
/// Nothing is charged before the customer accepts. Sending replaces the ticket's older open quote, and the chat line it
/// posts is a support reply, so a ticket nobody owns becomes the sender's (SupportService.ReplyAsync auto-claims).
/// </summary>
public sealed class SupportQuoteService(IUnitOfWork uow, ISupportQuoteStore store, IQuoteCheckout checkout, ISupportChat chat, IThaiClock clock)
{
    public async Task<SupportQuote> SendAsync(int actorId, int caseId, BookingForm form)
    {
        SupportQuote quote;
        await uow.BeginAsync();
        try
        {
            var ticket = await store.LockCaseAsync(caseId) ?? throw new KeyNotFoundException();
            if (ticket.Status == SupportStatus.Closed) throw new ArgumentException("ticket นี้ปิดแล้ว ส่งใบเสนอราคาไม่ได้");
            if (!await store.CustomerCanBookAsync(ticket.CustomerId)) throw new ArgumentException("บัญชีลูกค้านี้ทำรายการจองไม่ได้ (ถูกระงับหรือไม่ใช่บัญชีลูกค้า)");
            if (await store.HasActiveOrderAsync(ticket)) throw new ArgumentException("ticket นี้มีการจองอยู่แล้ว ยกเลิกตามเงื่อนไขก่อนเสนอราคาใหม่");
            var price = await checkout.PriceAsync(ticket.CustomerId, form);
            var now = clock.UtcNow;
            foreach (var older in await store.OpenQuotesAsync(caseId))
            {
                older.Status = QuoteStatus.Replaced;
                older.DecidedAtUtc = now;
            }
            quote = QuoteRules.Create(ticket, actorId, form, price, now);
            store.AddQuote(quote);
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
        await chat.PostAsync(actorId, caseId, QuoteRules.SentMessage(quote), fromStaff: true);
        return quote;
    }

    /// <summary>Books the quote. Returns the new order id and the ticket, so the page can go to either.</summary>
    public async Task<(int OrderId, int CaseId)> AcceptAsync(int customerId, int quoteId, QuotePayment payment)
    {
        var orderId = await checkout.BookAsync(customerId, quoteId, payment);
        var quote = await store.FindQuoteAsync(quoteId) ?? throw new KeyNotFoundException();
        await chat.PostAsync(customerId, quote.CaseId, QuoteRules.AcceptedMessage(quote, orderId, payment.Method), fromStaff: false);
        return (orderId, quote.CaseId);
    }

    public async Task<int> DeclineAsync(int customerId, int quoteId, string? reason)
    {
        SupportQuote quote;
        await uow.BeginAsync();
        try
        {
            quote = await store.LockQuoteAsync(quoteId) ?? throw new KeyNotFoundException();
            if (quote.CustomerId != customerId) throw new KeyNotFoundException();
            if (QuoteRules.DecisionError(quote, clock.UtcNow) is string error) throw new ArgumentException(error);
            quote.Status = QuoteStatus.Declined;
            quote.DecidedAtUtc = clock.UtcNow;
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch
        {
            await uow.RollbackAsync();
            throw;
        }
        await chat.PostAsync(customerId, quote.CaseId, QuoteRules.DeclinedMessage(quote, reason), fromStaff: false);
        return quote.CaseId;
    }
}
