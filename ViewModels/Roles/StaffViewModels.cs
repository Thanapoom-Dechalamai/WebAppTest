using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels.Roles;
public record StaffOrderRow(StayOrder Order, string CustomerName, string Email);
public record StaffDashboard(IReadOnlyList<StaffOrderRow> Orders, string? Search, string? Status)
{
    public IReadOnlyDictionary<string, string> HotelTeams { get; init; } = new Dictionary<string, string>();
    public int StaleCount { get; init; }
}
public class StaffCancellation
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(120)] public string Reason { get; set; } = "";
}
