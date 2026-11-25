namespace Nontiwat_7362_L002.ViewModels;

public class AccountIndexViewModel
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public IReadOnlyList<Nontiwat_7362_L002.Models.Promotion> Promotions { get; set; } = [];
    public IReadOnlyList<HotelCardViewModel> Hotels { get; set; } = [];
}
