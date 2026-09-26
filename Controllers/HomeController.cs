using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;

namespace FindIt.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalLost = await _context.Items.CountAsync(i => i.Type == ItemType.Lost);
        var totalFound = await _context.Items.CountAsync(i => i.Type == ItemType.Found);
        var totalResolved = await _context.Items.CountAsync(i => i.Status == ItemStatus.Resolved || i.Status == ItemStatus.Claimed);

        var recentItems = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.User)
            .OrderByDescending(i => i.CreatedAt)
            .Take(6)
            .ToListAsync();

        var categories = await _context.Categories
            .Include(c => c.Items)
            .ToListAsync();

        var model = new HomeViewModel
        {
            TotalLost = totalLost,
            TotalFound = totalFound,
            TotalResolved = totalResolved,
            RecentItems = recentItems,
            Categories = categories
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
