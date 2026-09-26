using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;
using FindIt.Services;

namespace FindIt.Controllers;

public class ItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmartMatchingService _matchingService;
    private readonly IFileStorageService _fileStorage;

    public ItemsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ISmartMatchingService matchingService,
        IFileStorageService fileStorage)
    {
        _context = context;
        _userManager = userManager;
        _matchingService = matchingService;
        _fileStorage = fileStorage;
    }

    // GET: /Items/Browse
    public async Task<IActionResult> Browse(
        string? search,
        int? categoryId,
        ItemType? type,
        ItemStatus? status,
        string? sortBy = "newest",
        int page = 1)
    {
        int pageSize = 9;

        var query = _context.Items
            .Include(i => i.Category)
            .Include(i => i.User)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(i => i.Title.ToLower().Contains(s) || 
                                     i.Description.ToLower().Contains(s) || 
                                     i.Location.ToLower().Contains(s));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(i => i.CategoryId == categoryId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        // Sorting
        query = sortBy switch
        {
            "oldest" => query.OrderBy(i => i.CreatedAt),
            "title" => query.OrderBy(i => i.Title),
            _ => query.OrderByDescending(i => i.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

        var model = new BrowseViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Type = type,
            Status = status,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = items,
            Categories = categories
        };

        return View(model);
    }

    // GET: /Items/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var item = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.User)
            .Include(i => i.Claims)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (item == null) return NotFound();

        // Run smart matching algorithm for recommendations
        var matches = await _matchingService.FindMatchesAsync(item);
        ViewBag.Matches = matches;

        // Current user check
        var currentUserId = _userManager.GetUserId(User);
        ViewBag.IsOwner = currentUserId != null && currentUserId == item.UserId;
        ViewBag.HasClaimed = currentUserId != null && item.Claims.Any(c => c.ClaimantId == currentUserId);

        return View(item);
    }

    // GET: /Items/Create
    [Authorize]
    public async Task<IActionResult> Create(ItemType? type)
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        var model = new ReportItemViewModel
        {
            Type = type ?? ItemType.Lost,
            DateLostOrFound = DateTime.Today
        };
        return View(model);
    }

    // POST: /Items/Create
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReportItemViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (ModelState.IsValid)
        {
            string? imageUrl = null;
            if (model.ImageFile != null)
            {
                imageUrl = await _fileStorage.SaveFileAsync(model.ImageFile, "items");
            }

            var item = new Item
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                CategoryId = model.CategoryId,
                Location = model.Location,
                DateLostOrFound = model.DateLostOrFound,
                SecretIdentifier = model.SecretIdentifier,
                ImageUrl = imageUrl,
                Status = ItemStatus.Open,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(item);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Your {(item.Type == ItemType.Lost ? "Lost" : "Found")} item report has been published successfully!";
            return RedirectToAction(nameof(Details), new { id = item.Id });
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
        return View(model);
    }

    // POST: /Items/Delete/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User);
        if (item.UserId != currentUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Item report removed.";
        return RedirectToAction("MyReports", "Dashboard");
    }
}
