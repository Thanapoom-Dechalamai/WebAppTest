using Nontiwat_7362_L002.Security;

namespace Nontiwat_7362_L002.Services.Booking;

/// <summary>
/// Who did what, in words: the label shown next to a booking event and the role checks team actions need.
/// <c>ActorId 0</c> is the system itself (the expiry sweep), so its label has no person behind it.
/// The real implementation is <c>Data/Booking/EfActorDirectory</c>; tests use fakes.
/// </summary>
public interface IActorDirectory
{
    /// <summary>How a booking's history says who acted: "Lodgo support · …", "พนักงานโรงแรม · …", "ระบบอัตโนมัติ" …</summary>
    Task<(string Label, string? Role)> DescribeAsync(int actorId);

    /// <summary>
    /// May this person act for this hotel? Admins may act for every hotel; everyone else must be
    /// on the hotel's team. Throws <see cref="UnauthorizedAccessException"/> when they are not.
    /// Returns the actor's role.
    /// </summary>
    Task<string?> EnsureHotelTeamMemberAsync(int actorId, IReadOnlyList<int> teamMemberIds);

    /// <summary>
    /// The team member who records the hotel's money: the acting HotelOwner when one acts,
    /// otherwise the first team member in id order (stable, so a booking's receipts add up).
    /// Null when the hotel has no HotelOwner member at all.
    /// </summary>
    Task<int?> HotelRecorderAsync(int actorId, string? actorRole, IReadOnlyList<int> teamMemberIds);
}
