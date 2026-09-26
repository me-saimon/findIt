using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;

namespace FindIt.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalItems = await _context.Items.CountAsync();
        var pendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);
        var activeDisputes = await _context.Disputes.CountAsync(d => d.Status == DisputeStatus.Pending);

        var recentItems = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.User)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentClaims = await _context.Claims
            .Include(c => c.Item)
            .Include(c => c.Claimant)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();

        var disputes = await _context.Disputes
            .Include(d => d.Claim)
                .ThenInclude(c => c!.Item)
            .Include(d => d.Claim)
                .ThenInclude(c => c!.Claimant)
            .Where(d => d.Status == DisputeStatus.Pending)
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalItems = totalItems,
            PendingClaims = pendingClaims,
            ActiveDisputes = activeDisputes,
            RecentItems = recentItems,
            RecentClaims = recentClaims,
            Disputes = disputes
        };

        return View(model);
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .Include(u => u.ReportedItems)
            .Include(u => u.SubmittedClaims)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(users);
    }

    // GET: /Admin/Disputes
    public async Task<IActionResult> Disputes(int? id)
    {
        var disputes = await _context.Disputes
            .Include(d => d.Claim)
                .ThenInclude(c => c!.Item)
                    .ThenInclude(i => i!.User)
            .Include(d => d.Claim)
                .ThenInclude(c => c!.Claimant)
            .ToListAsync();

        var activeDispute = id.HasValue
            ? disputes.FirstOrDefault(d => d.Id == id.Value)
            : disputes.FirstOrDefault();

        ViewBag.ActiveDispute = activeDispute;
        return View(disputes);
    }

    // POST: /Admin/ResolveDispute
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveDispute(int disputeId, DisputeStatus resolution, string notes)
    {
        var dispute = await _context.Disputes
            .Include(d => d.Claim)
                .ThenInclude(c => c!.Item)
            .FirstOrDefaultAsync(d => d.Id == disputeId);

        if (dispute == null) return NotFound();

        dispute.Status = resolution;
        dispute.ResolutionNotes = notes;
        dispute.ResolvedAt = DateTime.UtcNow;

        if (resolution == DisputeStatus.ResolvedInFavorOfClaimant)
        {
            dispute.Claim!.Status = ClaimStatus.Approved;
            dispute.Claim.Item!.Status = ItemStatus.Resolved;
        }
        else if (resolution == DisputeStatus.ResolvedInFavorOfFinder)
        {
            dispute.Claim!.Status = ClaimStatus.Rejected;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Dispute #{disputeId} marked as {resolution}.";
        return RedirectToAction(nameof(Disputes));
    }

    // POST: /Admin/ToggleUserStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            user.LockoutEnd = null; // Unban
            TempData["SuccessMessage"] = $"User {user.FullName} has been reactivated.";
        }
        else
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Ban
            TempData["SuccessMessage"] = $"User {user.FullName} has been suspended.";
        }

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Users));
    }
}
