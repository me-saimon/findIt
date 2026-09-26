using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;

namespace FindIt.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Messages?otherUserId=...&itemId=...
    public async Task<IActionResult> Index(string? otherUserId, int? itemId)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == null) return Challenge();

        // Find all users current user has chatted with
        var userMessages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Item)
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Distinct conversation partners
        var conversationPartners = userMessages
            .Select(m => m.SenderId == currentUserId ? m.Receiver : m.Sender)
            .Where(u => u != null)
            .DistinctBy(u => u!.Id)
            .ToList();

        // If otherUserId passed (e.g. from "Send Message" on Item Details)
        if (!string.IsNullOrEmpty(otherUserId) && !conversationPartners.Any(u => u!.Id == otherUserId))
        {
            var targetUser = await _userManager.FindByIdAsync(otherUserId);
            if (targetUser != null)
            {
                conversationPartners.Insert(0, targetUser);
            }
        }

        var activePartner = !string.IsNullOrEmpty(otherUserId)
            ? conversationPartners.FirstOrDefault(u => u!.Id == otherUserId)
            : conversationPartners.FirstOrDefault();

        var activeThread = new List<Message>();
        if (activePartner != null)
        {
            activeThread = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Item)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == activePartner.Id) ||
                            (m.SenderId == activePartner.Id && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Mark unread as read
            var unread = activeThread.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }
        }

        ViewBag.ActivePartner = activePartner;
        ViewBag.ActiveThread = activeThread;
        ViewBag.CurrentUserId = currentUserId;
        ViewBag.ActiveItemId = itemId;

        return View(conversationPartners);
    }

    // POST: /Messages/Send
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string receiverId, string content, int? itemId)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == null) return Challenge();

        if (!string.IsNullOrWhiteSpace(content))
        {
            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                ItemId = itemId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index), new { otherUserId = receiverId, itemId });
    }
}
