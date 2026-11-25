using System.Data;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.ViewModels;

namespace Nontiwat_7362_L002.Services.Booking;

public sealed class TeamBookingEdits(ApplicationDbContext db, IWalletStore wallets, IBookingStore store,
    IBookingJournal journal, IActorDirectory actors, IThaiClock clock, IWebHostEnvironment environment)
{
    public static bool EvidenceCanChange(StayOrder order) =>
        order.Status == BookingStatus.PaymentReview && PaymentRules.IsTransfer(order.PaymentMethod);

    public async Task SaveAsync(int actorId, int caseId, int customerId, TeamBookingEdit form)
    {
        string? added = null;
        string? old = null;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await wallets.LockWalletAsync(customerId);
            var item = await store.LockSupportCaseAsync(caseId);
            if (item is null || item.CustomerId != customerId || item.OrderId != form.OrderId || item.Status == SupportStatus.Closed)
                throw new ArgumentException("ไม่พบรายการที่แก้ไขได้ในแชตนี้");
            var order = await store.LoadOrderForUserAsync(form.OrderId, customerId);
            if (order is null || !BookingRules.CanCancel(order, clock.Today))
                throw new ArgumentException("สถานะปัจจุบันไม่อนุญาตให้แก้ไขรายการ");
            if (!BookingRules.PartyValid(order.Rooms, form.Guests)) throw new ArgumentException(BookingRules.PartyRule);
            if (form.Slip is not null && !EvidenceCanChange(order))
                throw new ArgumentException("แก้สลิปได้เฉพาะรายการโอนที่ยังรอตรวจ");
            if (EvidenceCanChange(order))
            {
                if (form.Slip is not null)
                {
                    added = await SaveSlipAsync(form.Slip, environment);
                    old = order.SlipFile;
                    order.SlipFile = added;
                }
                order.PaymentRef = PaymentRules.CleanPaymentRef(form.PaymentRef);
                if (!PaymentRules.TransferEvidenceGiven(order.PaymentRef, order.SlipFile))
                    throw new ArgumentException(PaymentRules.TransferEvidenceRule);
            }
            order.Guests = form.Guests;
            order.SpecialRequest = BookingRules.CleanRequest(form.SpecialRequest);
            var actor = await actors.DescribeAsync(actorId);
            await journal.AddEventAsync(order, order.Status, "เจ้าหน้าที่แก้ข้อมูลการจองตามคำขอ", actorId, actor.Label);
            await journal.NotifyCustomerAsync(order, "เจ้าหน้าที่แก้ข้อมูลการจองแล้ว", BookingText.Number(order.Id));
            await db.SaveChangesAsync();
        }
        catch
        {
            RemoveSlip(environment, added);
            throw;
        }
        await transaction.CommitAsync();
        RemoveSlip(environment, old);
    }

    public static async Task<string> SaveSlipAsync(IFormFile slip, IWebHostEnvironment environment)
    {
        var saved = await UploadedFiles.SaveAsync(slip, environment, "Slips");
        return saved.Name ?? throw new ArgumentException("สลิป: " + saved.Error);
    }

    public static void RemoveSlip(IWebHostEnvironment environment, string? name)
    {
        var path = UploadedFiles.PathOf(environment, "Slips", name);
        if (path is not null) File.Delete(path);
    }
}
