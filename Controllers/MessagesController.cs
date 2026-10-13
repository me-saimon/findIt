using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;
using FindIt.Models.ViewModels;

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

        // Find all messages involving the current user
        var allUserMessages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Item)
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Extract partner user IDs
        var partnerIds = allUserMessages
            .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Where(id => !string.IsNullOrEmpty(id) && id != currentUserId)
            .Distinct()
            .ToList();

        // If otherUserId passed (e.g. from "Send Message" on Item Details) and not in existing conversations
        if (!string.IsNullOrEmpty(otherUserId) && otherUserId != currentUserId && !partnerIds.Contains(otherUserId))
        {
            partnerIds.Insert(0, otherUserId);
        }

        // Fetch partner user objects
        var partnerUsers = await _userManager.Users
            .Where(u => partnerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        // Fetch active item if specified
        Item? activeItem = null;
        if (itemId.HasValue)
        {
            activeItem = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.Id == itemId.Value);
        }

        // Build rich conversation list items
        var conversations = new List<ConversationListItem>();
        foreach (var pid in partnerIds)
        {
            if (!partnerUsers.TryGetValue(pid, out var partner)) continue;

            var threadMsgs = allUserMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == pid) ||
                            (m.SenderId == pid && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToList();

            var lastMsg = threadMsgs.LastOrDefault();
            var unreadCount = threadMsgs.Count(m => m.SenderId == pid && m.ReceiverId == currentUserId && !m.IsRead);
            var relatedItem = threadMsgs.LastOrDefault(m => m.Item != null)?.Item;
            if (relatedItem == null && pid == otherUserId && activeItem != null)
            {
                relatedItem = activeItem;
            }

            conversations.Add(new ConversationListItem
            {
                Partner = partner,
                LastMessageContent = lastMsg?.Content,
                LastMessageTime = lastMsg?.SentAt,
                IsLastMessageFromMe = lastMsg?.SenderId == currentUserId,
                UnreadCount = unreadCount,
                RelatedItem = relatedItem
            });
        }

        // Order conversations: if new conversation with no messages yet, keep it at top, else order by last message time
        conversations = conversations
            .OrderByDescending(c => c.Partner.Id == otherUserId && c.LastMessageTime == null ? 1 : 0)
            .ThenByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
            .ToList();

        // Determine active partner
        ApplicationUser? activePartner = null;
        if (!string.IsNullOrEmpty(otherUserId) && partnerUsers.TryGetValue(otherUserId, out var requestedPartner))
        {
            activePartner = requestedPartner;
        }
        else if (conversations.Any())
        {
            activePartner = conversations.First().Partner;
        }

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

                var activeConv = conversations.FirstOrDefault(c => c.Partner.Id == activePartner.Id);
                if (activeConv != null) activeConv.UnreadCount = 0;
            }

            // If active item wasn't explicitly passed, check if activeThread has an item
            if (activeItem == null)
            {
                var lastItemMsg = activeThread.LastOrDefault(m => m.ItemId != null);
                if (lastItemMsg?.ItemId != null)
                {
                    activeItem = await _context.Items
                        .Include(i => i.Category)
                        .Include(i => i.User)
                        .FirstOrDefaultAsync(i => i.Id == lastItemMsg.ItemId.Value);
                }
            }
        }

        var viewModel = new MessagesViewModel
        {
            Conversations = conversations,
            ActivePartner = activePartner,
            ActiveThread = activeThread,
            CurrentUserId = currentUserId,
            ActiveItemId = activeItem?.Id ?? itemId,
            ActiveItem = activeItem
        };

        return View(viewModel);
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

