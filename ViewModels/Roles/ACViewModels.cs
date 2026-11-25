using System.ComponentModel.DataAnnotations;
using Nontiwat_7362_L002.Models;
namespace Nontiwat_7362_L002.ViewModels.Roles;
public record FinanceRow(WalletEntry Entry, string Email);
// Where the customers' money is right now, by booking status.
public record MoneyStatus(decimal Held, int HeldCount, decimal Deferred, int DeferredCount, decimal Earned, int EarnedCount, decimal Commission);
public record FinanceReport(DateTime From, DateTime To, decimal TopUps, decimal Payments, decimal Refunds, int Count, IReadOnlyList<FinanceRow> Entries)
{
    public MoneyStatus Status { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
}

public record SettlementRow(string HotelId, string HotelName, decimal Upcoming, decimal Earned, decimal Commission, decimal Paid)
{
    public decimal Payable => Earned - Commission;
    public decimal Outstanding => Payable - Paid;
}
public record PayoutRow(HotelPayout Payout, string PaidByEmail);
public record SettlementPage(IReadOnlyList<SettlementRow> Hotels, IReadOnlyList<PayoutRow> Payouts);
// One booking behind a hotel's balance: who paid, when, how much, and how it is split.
public record StatementOrder(StayOrder Order, string CustomerName, string CustomerEmail, DateTime? PaidAtUtc)
{
    public decimal Commission => FinanceRules.Commission(Order);
    public decimal HotelShare => FinanceRules.HotelShare(Order);
}
public record HotelStatementPage(SettlementRow Row, string City, IReadOnlyList<StatementOrder> Earned,
    IReadOnlyList<StatementOrder> Upcoming, IReadOnlyList<PayoutRow> Payouts);
public class PayoutForm
{
    [Required, StringLength(32)] public string HotelId { get; set; } = "";
    [Range(typeof(decimal), "0.01", "10000000")] public decimal Amount { get; set; }
    [Required, StringLength(120)] public string Reference { get; set; } = "";
    [Required, StringLength(20)] public string Method { get; set; } = "BankTransfer";
    [StringLength(255)] public string? Note { get; set; }
}

public record ReconcileRow(int UserId, string Email, decimal Balance, decimal LedgerTotal)
{
    public decimal Difference => Balance - LedgerTotal;
}
public record ReconcilePage(int WalletCount, IReadOnlyList<ReconcileRow> Mismatches);
