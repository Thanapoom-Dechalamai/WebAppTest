using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// The customer's demo credit wallet: the balance and the ledger lines under it.
/// Every booking and top-up locks the customer's wallet row first, so a resend or two purchases at the same
/// time cannot credit or debit the balance twice. The real implementation is <c>Data/Booking/EfWalletStore</c>;
/// tests use fakes. Ledger kinds and descriptions come from the pure rules (<c>PaymentRules</c>, <c>RefundPolicy</c>).
/// Nothing here saves: the caller's unit of work saves the whole action.
/// </summary>
public interface IWalletStore
{
    /// <summary>Create the wallet when it does not exist yet; a no-op when it does.</summary>
    Task EnsureWalletAsync(int userId);

    /// <summary>Lock the customer's wallet row for the duration of the current transaction.</summary>
    Task<MemberWallet> LockWalletAsync(int userId);

    /// <summary>Was a ledger line already written for the customer under this request id (the top-up idempotency check)?</summary>
    Task<bool> HasEntryAsync(int userId, string requestId);

    /// <summary>
    /// Move the balance and write the ledger line as one unit: <paramref name="amount"/> is added to the wallet
    /// (negative when money leaves) and the line records the balance that result leaves.
    /// </summary>
    Task AddEntryAsync(MemberWallet wallet, string requestId, string kind, decimal amount, string description);
}
