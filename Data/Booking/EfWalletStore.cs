using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Services.Booking;

namespace Nontiwat_7362_L002.Data.Booking;

/// <summary>
/// The EF implementation of <see cref="IWalletStore"/>. Every action runs inside the unit of work's
/// transaction, so nothing here saves on its own; the balance moves and the ledger line are one save.
/// </summary>
public sealed class EfWalletStore(ApplicationDbContext db) : IWalletStore
{
    public Task EnsureWalletAsync(int userId) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO member_wallets (UserId, Balance) VALUES ({userId}, 0) ON DUPLICATE KEY UPDATE UserId = UserId");

    // Locks the customer's wallet row; the caller holds the transaction.
    public async Task<MemberWallet> LockWalletAsync(int userId) =>
        (await db.Wallets.FromSqlInterpolated($"SELECT * FROM member_wallets WHERE UserId = {userId} FOR UPDATE").ToListAsync())
        .Single();

    public Task<bool> HasEntryAsync(int userId, string requestId) =>
        db.WalletEntries.AnyAsync(x => x.UserId == userId && x.RequestId == requestId);

    // Move the balance and write the ledger line as one unit: <paramref name="amount"/> is added to the wallet
    // (negative when money leaves) and the line records the balance that result leaves.
    public Task AddEntryAsync(MemberWallet wallet, string requestId, string kind, decimal amount, string description)
    {
        wallet.Balance += amount;
        db.WalletEntries.Add(new WalletEntry { UserId = wallet.UserId, RequestId = requestId, Kind = kind, Amount = amount,
            BalanceAfter = wallet.Balance, Description = description });
        return Task.CompletedTask;
    }
}
