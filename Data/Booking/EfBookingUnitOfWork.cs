using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nontiwat_7362_L002.Services.Booking;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// Owns the money-sensitive transaction: open it <c>ReadCommitted</c>, let the stores lock their rows,
/// save everything at once, then commit. Every money action runs inside one of these so that a failure
/// rolls the whole thing back. <c>CommitAsync</c> and <c>RollbackAsync</c> are safe to call more than once
/// (an idempotent path commits and returns, and a failed path rolls back then rethrows).
/// </summary>
public sealed class EfBookingUnitOfWork(ApplicationDbContext db) : IBookingUnitOfWork
{
    private IDbContextTransaction? _tx;

    public async Task BeginAsync() => _tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

    public Task SaveAsync() => db.SaveChangesAsync();

    public async Task CommitAsync()
    {
        if (_tx is null) return;
        await _tx.CommitAsync();
        _tx.Dispose();
        _tx = null;
    }

    public async Task RollbackAsync()
    {
        if (_tx is null) return;
        await _tx.RollbackAsync();
        _tx.Dispose();
        _tx = null;
    }

    public void ClearChangeTracker() => db.ChangeTracker.Clear();
}
