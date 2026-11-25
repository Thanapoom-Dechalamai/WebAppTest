using System.Data;
using Nontiwat_7362_L002.Services.Booking;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Models;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;
using Nontiwat_7362_L002.ViewModels.Roles;
namespace Nontiwat_7362_L002.Controllers;

// Finance: follows the customer's money from top-up, through Lodgo, to the hotel.
[Authorize(Policy = RolePolicies.AC)]
public class ACController(ApplicationDbContext db, Nontiwat_7362_L002.Services.SettlementService settlement, PaymentReviewService paymentReview, IThaiClock clock) : Controller
{
    // ---------- Every booking waits here for a money check before the hotel sees it (oldest first) ----------
    [HttpGet]
    public async Task<IActionResult> Payments()
    {
        var rows = await (from o in db.Orders.AsNoTracking() join u in db.Users.AsNoTracking() on o.UserId equals u.Id
                          where o.Status == BookingStatus.PaymentReview
                          orderby o.CreatedAtUtc
                          select new Nontiwat_7362_L002.ViewModels.PaymentReviewRow(o, u.FirstName + " " + u.LastName, u.Email)).Take(200).ToListAsync();
        // A transfer reference already used on another booking that is not cancelled is flagged for a closer look.
        var refs = rows.Where(x => x.Order.PaymentRef != null).Select(x => x.Order.PaymentRef!).Distinct().ToList();
        var used = await db.Orders.AsNoTracking().Where(x => x.PaymentRef != null && refs.Contains(x.PaymentRef) && x.Status != BookingStatus.Cancelled)
            .GroupBy(x => x.PaymentRef!).Where(g => g.Count() > 1).Select(g => g.Key).ToListAsync();
        ViewData["DuplicateRefs"] = used.ToHashSet();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayment(int id)
    {
        try
        {
            await paymentReview.ApprovePaymentAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id);
            TempData["Success"] = $"ยืนยันการชำระเงิน LG{id:D6} แล้ว ส่งต่อให้โรงแรมยืนยันห้อง";
        }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Payments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int id, string? reason)
    {
        try
        {
            await paymentReview.RejectPaymentAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, reason);
            TempData["Success"] = $"แจ้งลูกค้าว่าการชำระเงิน LG{id:D6} ไม่ผ่านแล้ว รายการถูกยกเลิก";
        }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return RedirectToAction(nameof(Payments));
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        if (!ModelState.IsValid) return BadRequest("วันที่ไม่ถูกต้อง");
        if (Range(from, to) is not { } range) return BadRequest(RangeError);
        var (start, end) = range;
        var query = Ledger(start, end);
        var topUps = await query.Where(x => x.Kind == "TopUp").SumAsync(x => (decimal?)x.Amount) ?? 0;
        var payments = -(await query.Where(x => x.Kind == "Payment").SumAsync(x => (decimal?)x.Amount) ?? 0);
        var refunds = await query.Where(x => x.Kind == "Refund").SumAsync(x => (decimal?)x.Amount) ?? 0;
        var entries = await (from entry in query join user in db.Users.AsNoTracking() on entry.UserId equals user.Id
                            orderby entry.Id descending select new FinanceRow(entry, user.Email)).Take(200).ToListAsync();
        // Snapshot of all bookings, not limited to the date range.
        var byStatus = await db.Orders.AsNoTracking().GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() }).ToListAsync();
        decimal TotalOf(params string[] s) => byStatus.Where(x => s.Contains(x.Status)).Sum(x => x.Total);
        int CountOf(params string[] s) => byStatus.Where(x => s.Contains(x.Status)).Sum(x => x.Count);
        var earnedTotals = (await db.Orders.AsNoTracking().Where(x => FinanceRules.EarnedStatuses.Contains(x.Status)).Select(x => new { x.Total, x.CommissionPercent }).ToListAsync())
            .Select(x => (x.Total, x.CommissionPercent)).ToList();
        var status = new MoneyStatus(TotalOf(BookingStatus.Pending), CountOf(BookingStatus.Pending),
            TotalOf(FinanceRules.DeferredStatuses), CountOf(FinanceRules.DeferredStatuses),
            TotalOf(FinanceRules.EarnedStatuses), CountOf(FinanceRules.EarnedStatuses), FinanceRules.CommissionOf(earnedTotals));
        return View(new FinanceReport(start, end, topUps, payments, refunds, await query.CountAsync(), entries) { Status = status });
    }

    // ---------- Shared by the ledger page and its CSV ----------
    private const string RangeError = "เลือกวันที่เริ่มและสิ้นสุดให้ถูกต้อง ช่วงไม่เกิน 366 วัน";

    // Default: first day of this month to today (Thai dates). Null when the range is not allowed.
    private (DateTime Start, DateTime End)? Range(DateTime? from, DateTime? to)
    {
        var today = clock.Today;
        var start = from?.Date ?? new DateTime(today.Year, today.Month, 1);
        var end = to?.Date ?? today;
        return start.Year < 2000 || end.Year > 2100 || end < start || (end - start).Days > 366 ? null : (start, end);
    }

    private IQueryable<WalletEntry> Ledger(DateTime start, DateTime end)
    {
        var utcStart = start.AddHours(-7);
        var utcEnd = end.AddDays(1).AddHours(-7);
        return db.WalletEntries.AsNoTracking().Where(x => x.CreatedAtUtc >= utcStart && x.CreatedAtUtc < utcEnd);
    }

    // CSV for Excel: kind=ledger (wallet entries in the date range, all of them) or kind=settlement (per hotel, all time).
    [HttpGet]
    public async Task<IActionResult> Export(string? kind, DateTime? from, DateTime? to)
    {
        if (kind == "ledger")
        {
            if (!ModelState.IsValid || Range(from, to) is not { } range) return BadRequest(RangeError);
            var (start, end) = range;
            var rows = await (from e in Ledger(start, end) join u in db.Users.AsNoTracking() on e.UserId equals u.Id
                              orderby e.CreatedAtUtc, e.Id
                              select new { e.CreatedAtUtc, u.Email, Name = u.FirstName + " " + u.LastName, e.Kind, e.Amount, e.BalanceAfter, e.Description, e.RequestId })
                             .ToListAsync();
            var bytes = CsvExport.Build(["วันที่ (เวลาไทย)", "อีเมล", "ชื่อลูกค้า", "ประเภท", "จำนวนเงิน", "ยอดคงเหลือหลังรายการ", "รายละเอียด", "RequestId"],
                rows.Select(r => new object?[] { r.CreatedAtUtc.AddHours(7), r.Email, r.Name, KindLabel(r.Kind), r.Amount, r.BalanceAfter, r.Description, r.RequestId }));
            return File(bytes, "text/csv; charset=utf-8", $"lodgo-ledger-{start:yyyy-MM-dd}-{end:yyyy-MM-dd}.csv");
        }
        if (kind == "settlement")
        {
            var rows = await settlement.RowsAsync(null);
            var bytes = CsvExport.Build(["รหัสโรงแรม", "ชื่อโรงแรม", "รอเข้าพัก", "ยอดที่เกิดแล้ว", "ค่าคอมมิชชัน", "ต้องโอนให้โรงแรม", "โอนแล้ว", "ค้างโอน"],
                rows.Select(r => new object?[] { r.HotelId, r.HotelName, r.Upcoming, r.Earned, r.Commission, r.Payable, r.Paid, r.Outstanding }));
            return File(bytes, "text/csv; charset=utf-8", $"lodgo-settlement-{clock.Today:yyyy-MM-dd}.csv");
        }
        return BadRequest("เลือกชนิดไฟล์ ledger หรือ settlement");
    }

    private static string KindLabel(string kind) => kind switch
    {
        "TopUp" => "เติมเครดิต", "Payment" => "ชำระค่าจอง", "Refund" => "คืนเครดิต", _ => kind
    };

    [HttpGet]
    public async Task<IActionResult> Settlement() => View(await SettlementModel());

    private async Task<SettlementPage> SettlementModel()
    {
        var rows = await settlement.RowsAsync(null);
        return new SettlementPage(rows, await PayoutRowsAsync(null, 100));
    }

    private async Task<List<PayoutRow>> PayoutRowsAsync(string? hotelId, int take) =>
        await (from p in db.HotelPayouts.AsNoTracking() join u in db.Users.AsNoTracking() on p.PaidByUserId equals u.Id
               where hotelId == null || p.HotelId == hotelId
               orderby p.Id descending select new PayoutRow(p, u.Email)).Take(take).ToListAsync();

    // Where a hotel's money comes from: each finished booking (customer, when they paid, the split),
    // bookings still waiting to finish, and every transfer Lodgo has made to the hotel.
    [HttpGet]
    public async Task<IActionResult> Statement(string id)
    {
        var hotel = await db.Hotels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (hotel is null) return NotFound();
        var row = (await settlement.RowsAsync(id)).SingleOrDefault() ?? new SettlementRow(id, hotel.Name, 0, 0, 0, 0);
        var orders = await (from o in db.Orders.AsNoTracking() join u in db.Users.AsNoTracking() on o.UserId equals u.Id
                            where o.HotelId == id && (FinanceRules.EarnedStatuses.Contains(o.Status) || FinanceRules.DeferredStatuses.Contains(o.Status))
                            orderby o.CheckIn descending, o.Id descending
                            select new { Order = o, Name = u.FirstName + " " + u.LastName, u.Email }).Take(300).ToListAsync();
        var keys = orders.Select(x => "order:" + x.Order.RequestId).ToList();
        var paidAt = (await db.WalletEntries.AsNoTracking().Where(x => x.Kind == "Payment" && keys.Contains(x.RequestId))
            .Select(x => new { x.UserId, x.RequestId, x.CreatedAtUtc }).ToListAsync())
            .GroupBy(x => (x.UserId, x.RequestId)).ToDictionary(g => g.Key, g => g.Min(x => x.CreatedAtUtc));
        var lines = orders.Select(x => new StatementOrder(x.Order, x.Name, x.Email,
            paidAt.TryGetValue((x.Order.UserId, "order:" + x.Order.RequestId), out var at) ? at : null)).ToList();
        return View(new HotelStatementPage(row, hotel.City,
            lines.Where(x => FinanceRules.EarnedStatuses.Contains(x.Order.Status)).ToList(),
            lines.Where(x => FinanceRules.DeferredStatuses.Contains(x.Order.Status)).ToList(),
            await PayoutRowsAsync(id, 200)));
    }


    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayout(PayoutForm form)
    {
        if (!ModelState.IsValid || decimal.Round(form.Amount, 2) != form.Amount || !FinanceRules.PayoutMethods.ContainsKey(form.Method))
        { TempData["Error"] = "ระบุยอดโอน (ทศนิยมไม่เกิน 2 ตำแหน่ง) วิธีโอน และเลขอ้างอิงการโอน"; return RedirectToAction(nameof(Statement), new { id = form.HotelId }); }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Lock the hotel row so two people cannot record the same payout at once.
        var hotel = (await db.Hotels.FromSqlInterpolated($"SELECT * FROM hotels WHERE Id = {form.HotelId} FOR UPDATE").AsNoTracking().ToListAsync()).SingleOrDefault();
        if (hotel is null) return NotFound();
        var row = (await settlement.RowsAsync(hotel.Id)).SingleOrDefault();
        var outstanding = row?.Outstanding ?? 0;
        if (form.Amount > outstanding)
        { TempData["Error"] = $"ยอดโอนเกินยอดค้างจ่ายของ {hotel.Name} (฿{outstanding:N2})"; return RedirectToAction(nameof(Statement), new { id = hotel.Id }); }
        db.HotelPayouts.Add(new HotelPayout { HotelId = hotel.Id, HotelName = hotel.Name, Amount = form.Amount,
            Reference = form.Reference.Trim(), Method = form.Method, Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(), PaidByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Success"] = $"บันทึกการโอน ฿{form.Amount:N2} ให้ {hotel.Name} แล้ว ({FinanceRules.MethodLabel(form.Method)} · อ้างอิง {form.Reference.Trim()})";
        return RedirectToAction(nameof(Statement), new { id = hotel.Id });
    }

    // Every wallet balance must equal the sum of its ledger entries.
    [HttpGet]
    public async Task<IActionResult> Reconcile()
    {
        var wallets = await (from w in db.Wallets.AsNoTracking()
                             join u in db.Users.AsNoTracking() on w.UserId equals u.Id
                             select new ReconcileRow(w.UserId, u.Email, w.Balance,
                                 db.WalletEntries.Where(entry => entry.UserId == w.UserId).Sum(entry => (decimal?)entry.Amount) ?? 0m))
                             .ToListAsync();
        var mismatches = wallets.Where(x => x.Difference != 0)
            .OrderByDescending(x => Math.Abs(x.Difference)).ThenBy(x => x.UserId).ToList();
        return View(new ReconcilePage(wallets.Count, mismatches));
    }
}
