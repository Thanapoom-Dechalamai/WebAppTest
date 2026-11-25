using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;

namespace Nontiwat_7362_L002.ViewModels;

public record UserRowViewModel(int Id, string Email, string FirstName, string LastName,
    string Role, DateTime CreatedAtUtc)
{
    public bool IsSuspended { get; init; }
    public string? SuspendedReason { get; init; }
    public int HotelCount { get; init; }
    public DateTime? LastLoginUtc { get; init; }
    public string Initials => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Initial(Email)
        : Initial(FirstName) + Initial(LastName);

    private static string Initial(string value) => StringInfo.GetNextTextElement(value.Trim()).ToUpperInvariant();
}

public record UserDirectoryQuery(string? Search, string? Role, string? Status, int Page)
{
    public static UserDirectoryQuery Normalize(string? search, string? role, string? status, int page)
    {
        search = search?.Trim();
        if (search?.Length > 100) search = search[..100];
        return new(search, AppRoles.IsValid(role) ? role : null, status is "active" or "suspended" ? status : null, page);
    }

    public IQueryable<ApplicationUser> Apply(IQueryable<ApplicationUser> users)
    {
        if (!string.IsNullOrEmpty(Search))
        {
            var term = Search;
            users = users.Where(x => x.Email.Contains(term) || x.FirstName.Contains(term) ||
                x.LastName.Contains(term) || (x.FirstName + " " + x.LastName).Contains(term));
        }
        if (Role is not null) users = users.Where(x => x.Role == Role);
        if (Status is not null)
        {
            var suspended = Status == "suspended";
            users = users.Where(x => x.IsSuspended == suspended);
        }
        return users;
    }

    public static int PageCount(int total, int pageSize) => Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

    public int ClampPage(int total, int pageSize) => Math.Clamp(Page, 1, PageCount(total, pageSize));
}

public class UserListViewModel
{
    public string? Search { get; set; }
    public IReadOnlyList<UserRowViewModel> Users { get; set; } = [];
    public string? RoleFilter { get; init; }
    public string? StatusFilter { get; init; }
    public int Page { get; init; } = 1;
    public int PageCount { get; init; } = 1;
    public int Total { get; init; }
}

public class ChangeRoleViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required]
    [RegularExpression(Nontiwat_7362_L002.Security.AppRoles.Pattern)]
    public string Role { get; set; } = "User";
}
