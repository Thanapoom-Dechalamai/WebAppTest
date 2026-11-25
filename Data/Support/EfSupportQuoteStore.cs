using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services.Support;

namespace Nontiwat_7362_L002.Data.Support;

/// <summary>EF implementation of <see cref="ISupportQuoteStore"/>. Shares the request's DbContext with the unit of work.</summary>
public sealed class EfSupportQuoteStore(ApplicationDbContext db) : ISupportQuoteStore
{
    public async Task<SupportCase?> LockCaseAsync(int caseId) =>
        (await db.SupportCases.FromSqlInterpolated($"SELECT * FROM support_cases WHERE Id = {caseId} FOR UPDATE").ToListAsync()).SingleOrDefault();

    public Task<bool> CustomerCanBookAsync(int customerId) =>
        db.Users.AnyAsync(x => x.Id == customerId && x.Role == AppRoles.Customer && !x.IsSuspended);

    public Task<bool> HasActiveOrderAsync(SupportCase ticket) => ticket.OrderId is int orderId
        ? db.Orders.AnyAsync(x => x.Id == orderId && x.Status != BookingStatus.Cancelled)
        : Task.FromResult(false);

    public async Task<IReadOnlyList<SupportQuote>> OpenQuotesAsync(int caseId) =>
        await db.SupportQuotes.Where(x => x.CaseId == caseId && x.Status == QuoteStatus.Open).ToListAsync();

    public async Task<SupportQuote?> LockQuoteAsync(int quoteId) =>
        (await db.SupportQuotes.FromSqlInterpolated($"SELECT * FROM support_quotes WHERE Id = {quoteId} FOR UPDATE").ToListAsync()).SingleOrDefault();

    public Task<SupportQuote?> FindQuoteAsync(int quoteId) => db.SupportQuotes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == quoteId);

    public void AddQuote(SupportQuote quote) => db.SupportQuotes.Add(quote);
}
