using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nontiwat_7362_L002.Data;
using Nontiwat_7362_L002.Security;
using Nontiwat_7362_L002.Services;

namespace Nontiwat_7362_L002.Controllers;

// Requests from hotel staff to look after a hotel. Admin and Manager decide; the requester may open their own proof.
[Authorize]
public class HotelRequestsController(ApplicationDbContext db, HotelAccessService access) : Controller
{
    private const string Reviewers = AppRoles.Admin + "," + AppRoles.Manager;
    private int ActorId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("/HotelRequests"), Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Index() => View(await access.QueueAsync());

    // One request: who asked, the proof, and the decision (pending ones carry the approve / reject forms)
    [HttpGet("/HotelRequests/{id:int}"), Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Details(int id)
    {
        var row = await access.FindAsync(id);
        return row is null ? NotFound() : View(row);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Approve(int id)
    {
        try { await access.ApproveAsync(ActorId, id); TempData["Success"] = "อนุมัติแล้ว พนักงานคนนี้เห็นการจองของโรงแรมนั้นได้ทันที"; }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return Redirect("/HotelRequests");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Reviewers)]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        try { await access.RejectAsync(ActorId, id, reason); TempData["Success"] = "บันทึกว่าไม่อนุมัติ และแจ้งเหตุผลให้ผู้ขอแล้ว"; }
        catch (ArgumentException e) { TempData["Error"] = e.Message; }
        return Redirect("/HotelRequests");
    }

    [HttpGet]
    public async Task<IActionResult> Proof(int id, [FromServices] IWebHostEnvironment env)
    {
        var request = await db.HotelAccessRequests.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.UserId, x.ProofFile }).SingleOrDefaultAsync();
        if (request is null) return NotFound();
        if (request.UserId != ActorId && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.Manager)) return Forbid();
        var path = UploadedFiles.PathOf(env, "HotelProofs", request.ProofFile);
        if (path is null) return request.ProofFile is null ? NotFound() : this.FileMissing();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, UploadedFiles.ContentType(path));
    }
}
