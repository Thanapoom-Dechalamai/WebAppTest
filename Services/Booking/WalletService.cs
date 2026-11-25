using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class WalletService(IBookingUnitOfWork uow, IWalletStore walletStore)
{
    public Task EnsureWalletAsync(int userId) => walletStore.EnsureWalletAsync(userId);

    public async Task TopUpAsync(int userId, TopUpForm form)
    {
        if (!PaymentRules.TopUpValid(form)) throw new ArgumentException(PaymentRules.TopUpAmountRule);
        await walletStore.EnsureWalletAsync(userId);
        await uow.BeginAsync();
        try
        {
            var wallet = await walletStore.LockWalletAsync(userId);
            var key = "topup:" + form.RequestId;
            if (await walletStore.HasEntryAsync(userId, key)) { await uow.CommitAsync(); return; }
            if (!PaymentRules.TopUpFitsBalance(wallet.Balance, form.Amount)) throw new ArgumentException(PaymentRules.MaxBalanceRule);
            await walletStore.AddEntryAsync(wallet, key, "TopUp", form.Amount, PaymentRules.TopUpDescription);
            await uow.SaveAsync();
            await uow.CommitAsync();
        }
        catch { await uow.RollbackAsync(); throw; }
    }
}
