namespace Nontiwat_7362_L002.Services;

/// <summary>
/// One database transaction around one action: open it before locking any row (<c>SELECT ... FOR UPDATE</c>),
/// stage the changes, save once, then commit. A failure in between is rolled back, so nothing is half done.
/// The EF implementation is <c>Data/Booking/EfBookingUnitOfWork</c> (ReadCommitted); tests use NSubstitute fakes.
/// Booking and money actions use <see cref="Booking.IBookingUnitOfWork"/>, which adds one booking-only method.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Open the transaction. Call before locking any row.</summary>
    Task BeginAsync();

    /// <summary>Persist the staged changes inside the transaction.</summary>
    Task SaveAsync();

    /// <summary>Commit and release the transaction. Idempotent: a no-op if nothing is open.</summary>
    Task CommitAsync();

    /// <summary>Undo the transaction. Idempotent: a no-op if nothing is open.</summary>
    Task RollbackAsync();
}
