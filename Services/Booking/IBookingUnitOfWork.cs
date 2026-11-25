namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// The transaction a money action runs in: opened <c>ReadCommitted</c> before any row is locked, saved once
/// all changes are staged, then committed. One of these per action, so a failure anywhere rolls the whole
/// booking back. Begin/Save/Commit/Rollback come from <see cref="IUnitOfWork"/>. The real implementation is
/// <c>Data/Booking/EfBookingUnitOfWork</c>; tests use fakes that record when each step was called.
/// </summary>
public interface IBookingUnitOfWork : IUnitOfWork
{
    /// <summary>Forget EF's tracked rows so the next read is fresh. Used between one order and the next in a batch.</summary>
    void ClearChangeTracker();
}
