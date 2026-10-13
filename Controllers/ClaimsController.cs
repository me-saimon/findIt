using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;
using FindIt.Services;

namespace FindIt.Controllers;

[Authorize]
public class ClaimsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _fileStorage;

    public ClaimsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileStorageService fileStorage)
    {
        _context = context;
        _userManager = userManager;
        _fileStorage = fileStorage;
    }

    // GET: /Claims
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        // Claims submitted by me
        var myClaims = await _context.Claims
            .Include(c => c.Item)
                .ThenInclude(i => i!.Category)
            .Include(c => c.Item)
                .ThenInclude(i => i!.User)
            .Where(c => c.ClaimantId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        // Incoming claims on items I reported
        var incomingClaims = await _context.Claims
            .Include(c => c.Item)
                .ThenInclude(i => i!.Category)
            .Include(c => c.Claimant)
            .Where(c => c.Item != null && c.Item.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.IncomingClaims = incomingClaims;
        return View(myClaims);
    }

    // GET: /Claims/Submit/5
    public async Task<IActionResult> Submit(int itemId)
    {
        var item = await _context.Items
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (item.UserId == userId)
        {
            TempData["ErrorMessage"] = "You cannot claim an item you reported yourself.";
            return RedirectToAction("Details", "Items", new { id = itemId });
        }

        var existingClaim = await _context.Claims
            .FirstOrDefaultAsync(c => c.ItemId == itemId && c.ClaimantId == userId);
        if (existingClaim != null)
        {
            TempData["InfoMessage"] = "You have already submitted a claim for this item. Please check your claims dashboard.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ClaimSubmissionViewModel
        {
            ItemId = item.Id,
            ItemTitle = item.Title,
            ItemLocation = item.Location,
            SecretQuestionPrompt = item.SecretIdentifier
        };

        return View(model);
    }

    // POST: /Claims/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ClaimSubmissionViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (ModelState.IsValid)
        {
            string? proofUrl = null;
            if (model.ProofImageFile != null)
            {
                proofUrl = await _fileStorage.SaveFileAsync(model.ProofImageFile, "claims");
            }

            var claim = new Claim
            {
                ItemId = model.ItemId,
                ClaimantId = user.Id,
                SecretAnswer = model.SecretAnswer,
                ProofDescription = model.ProofDescription,
                ProofImageUrl = proofUrl,
                Status = ClaimStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your ownership claim has been submitted! The finder has been notified.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: /Claims/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var claim = await _context.Claims
            .Include(c => c.Claimant)
            .Include(c => c.Item)
                .ThenInclude(i => i!.Category)
            .Include(c => c.Item)
                .ThenInclude(i => i!.User)
            .Include(c => c.Dispute)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim == null) return NotFound();

        bool isClaimant = claim.ClaimantId == userId;
        bool isFinder = claim.Item != null && claim.Item.UserId == userId;
        bool isAdmin = User.IsInRole("Admin");

        if (!isClaimant && !isFinder && !isAdmin)
        {
            return Forbid();
        }

        ViewBag.IsClaimant = isClaimant;
        ViewBag.IsFinder = isFinder;
        ViewBag.IsAdmin = isAdmin;
        ViewBag.CurrentUserId = userId;

        return View(claim);
    }

    // POST: /Claims/Review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int claimId, ClaimStatus status, string? reviewNotes, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        var claim = await _context.Claims
            .Include(c => c.Item)
            .FirstOrDefaultAsync(c => c.Id == claimId);

        if (claim == null || claim.Item == null) return NotFound();

        // Ensure current user is the finder / reporter of the item or admin
        if (claim.Item.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        claim.Status = status;
        claim.ReviewNotes = reviewNotes;
        claim.ReviewedAt = DateTime.UtcNow;

        if (status == ClaimStatus.Approved)
        {
            claim.Item.Status = ItemStatus.Resolved;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Claim #{claim.Id:D5} status has been updated to {status}.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id = claimId });
    }

    // POST: /Claims/Dispute
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispute(int claimId, string reason)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Challenge();

        var claim = await _context.Claims
            .Include(c => c.Dispute)
            .FirstOrDefaultAsync(c => c.Id == claimId);

        if (claim == null) return NotFound();
        if (claim.ClaimantId != userId && !User.IsInRole("Admin")) return Forbid();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please provide an explanation to file a dispute.";
            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        if (claim.Dispute != null)
        {
            TempData["InfoMessage"] = "A dispute has already been filed for this claim.";
            return RedirectToAction(nameof(Details), new { id = claimId });
        }

        var dispute = new Dispute
        {
            ClaimId = claimId,
            Reason = reason.Trim(),
            Status = DisputeStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        claim.Status = ClaimStatus.Disputed;
        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your dispute has been logged. Our moderation team will review this case shortly.";
        return RedirectToAction(nameof(Details), new { id = claimId });
    }
}
