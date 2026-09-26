using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;

namespace FindIt.Services;

public class SmartMatchingService : ISmartMatchingService
{
    private readonly ApplicationDbContext _context;

    public SmartMatchingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MatchResult>> FindMatchesAsync(Item sourceItem, int limit = 5)
    {
        // Search for items of the OPPOSITE type (if Lost, look for Found; if Found, look for Lost)
        var oppositeType = sourceItem.Type == ItemType.Lost ? ItemType.Found : ItemType.Lost;

        var candidates = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.User)
            .Where(i => i.Id != sourceItem.Id && i.Type == oppositeType && i.Status == ItemStatus.Open)
            .ToListAsync();

        var scored = new List<MatchResult>();

        var sourceTitleWords = ExtractWords(sourceItem.Title);
        var sourceLocationWords = ExtractWords(sourceItem.Location);

        foreach (var cand in candidates)
        {
            int score = 0;
            var reasons = new List<string>();

            // 1. Category Match (40%)
            if (cand.CategoryId == sourceItem.CategoryId)
            {
                score += 40;
                reasons.Add("Identical category match");
            }

            // 2. Location Similarity (30%)
            var candLocationWords = ExtractWords(cand.Location);
            int locationOverlap = sourceLocationWords.Intersect(candLocationWords, StringComparer.OrdinalIgnoreCase).Count();
            if (locationOverlap > 0)
            {
                int locScore = Math.Min(30, locationOverlap * 15);
                score += locScore;
                reasons.Add($"Proximity / Location match ({locationOverlap} common keyword{(locationOverlap > 1 ? "s" : "")})");
            }

            // 3. Date Proximity (15%)
            int dayDiff = Math.Abs((cand.DateLostOrFound.Date - sourceItem.DateLostOrFound.Date).Days);
            if (dayDiff == 0)
            {
                score += 15;
                reasons.Add("Same day report");
            }
            else if (dayDiff <= 2)
            {
                score += 10;
                reasons.Add("Reported within 48 hours");
            }
            else if (dayDiff <= 7)
            {
                score += 5;
                reasons.Add("Reported within a week");
            }

            // 4. Keyword / Title overlap (15%)
            var candTitleWords = ExtractWords(cand.Title);
            int titleOverlap = sourceTitleWords.Intersect(candTitleWords, StringComparer.OrdinalIgnoreCase).Count();
            if (titleOverlap > 0)
            {
                int titleScore = Math.Min(15, titleOverlap * 8);
                score += titleScore;
                reasons.Add($"Title similarity ({titleOverlap} matched term{(titleOverlap > 1 ? "s" : "")})");
            }

            // Only consider if score is at least 30%
            if (score >= 30)
            {
                scored.Add(new MatchResult
                {
                    CandidateItem = cand,
                    Score = Math.Min(100, score),
                    Reason = string.Join("; ", reasons)
                });
            }
        }

        return scored
            .OrderByDescending(s => s.Score)
            .Take(limit)
            .ToList();
    }

    private static HashSet<string> ExtractWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>();

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "in", "on", "at", "to", "for", "with", "by", "of", "and", "or", "is", "it", "my", "near"
        };

        var words = text.Split(new[] { ' ', ',', '.', '-', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        return words
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
