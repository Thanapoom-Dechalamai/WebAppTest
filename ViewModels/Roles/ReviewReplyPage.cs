using Nontiwat_7362_L002.Models;

namespace Nontiwat_7362_L002.ViewModels.Roles;

// HotelStaff page that lists a hotel's reviews and lets the team reply.
public record ReviewReplyPage(IReadOnlyList<HotelCardViewModel> Hotels, HotelCardViewModel? Hotel, string Filter,
    IReadOnlyList<HotelReview> Items, int UnansweredCount, int Total)
{
    public const int PageSize = 50;
    public const string Unanswered = "unanswered";
    public const string All = "all";
}
