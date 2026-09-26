using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;

namespace FindIt.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public DashboardController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /Dashboard
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var myReports = await _context.Items
            .Include(i => i.Category)
            .Where(i => i.UserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        var myClaims = await _context.Claims
            .Include(c => c.Item)
            .Where(c => c.ClaimantId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();

        var totalReports = await _context.Items.CountAsync(i => i.UserId == user.Id);
        var totalClaims = await _context.Claims.CountAsync(c => c.ClaimantId == user.Id);
        var totalResolved = await _context.Items.CountAsync(i => i.UserId == user.Id && (i.Status == ItemStatus.Resolved || i.Status == ItemStatus.Claimed));

        var model = new DashboardViewModel
        {
            User = user,
            MyReportCount = totalReports,
            MyClaimCount = totalClaims,
            ResolvedCount = totalResolved,
            MyRecentReports = myReports,
            MyRecentClaims = myClaims
        };

        return View(model);
    }

    // GET: /Dashboard/MyReports
    public async Task<IActionResult> MyReports()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var items = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.Claims)
            .Where(i => i.UserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    // GET: /Dashboard/Settings
    public async Task<IActionResult> Settings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        return View(user);
    }

    // POST: /Dashboard/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string departmentOrRole, string phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.FullName = fullName;
        user.DepartmentOrRole = departmentOrRole;
        user.PhoneNumber = phoneNumber;

        await _userManager.UpdateAsync(user);
        TempData["SuccessMessage"] = "Profile details updated successfully.";

        return RedirectToAction(nameof(Settings));
    }

    // POST: /Dashboard/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "New passwords do not match.";
            return RedirectToAction(nameof(Settings));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Your password has been changed.";
        }
        else
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Settings));
    }
}
