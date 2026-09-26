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

    // POST: /Claims/Review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int claimId, ClaimStatus status, string? reviewNotes)
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

        TempData["SuccessMessage"] = $"Claim has been marked as {status}.";
        return RedirectToAction(nameof(Index));
    }
}
