using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using FindIt.Data;
using FindIt.Models;

namespace FindIt.Services;

public class SmartMatchingService : ISmartMatchingService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmartMatchingService> _logger;

    public SmartMatchingService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SmartMatchingService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
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

        if (!candidates.Any())
        {
            return new List<MatchResult>();
        }

        // 1. Try Google Gemini AI matching first if API key is configured
        var geminiApiKey = _configuration["Gemini:ApiKey"];
        if (!string.IsNullOrWhiteSpace(geminiApiKey))
        {
            try
            {
                var aiMatches = await MatchWithGeminiAsync(sourceItem, candidates, geminiApiKey, limit);
                if (aiMatches.Any())
                {
                    return aiMatches;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini AI matching encountered an issue. Falling back to local heuristic matching.");
            }
        }

        // 2. Fallback to local heuristic matching
        return MatchWithLocalHeuristic(sourceItem, candidates, limit);
    }

    private async Task<List<MatchResult>> MatchWithGeminiAsync(
        Item sourceItem, 
        List<Item> candidates, 
        string apiKey, 
        int limit)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);

        var primaryModel = _configuration["Gemini:Model"] ?? "gemini-flash-lite-latest";
        var fallbackModel = _configuration["Gemini:FallbackModel"] ?? "gemini-flash-latest";

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are an intelligent Lost & Found matching assistant for a campus community.");
        promptBuilder.AppendLine("Analyze whether any of the candidate items correspond to the source item.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"SOURCE ITEM (Status: {sourceItem.Status}, Type: {sourceItem.Type}):");
        promptBuilder.AppendLine($"- ID: {sourceItem.Id}");
        promptBuilder.AppendLine($"- Title: {sourceItem.Title}");
        promptBuilder.AppendLine($"- Category: {sourceItem.Category?.Name ?? "General"}");
        promptBuilder.AppendLine($"- Location: {sourceItem.Location}");
        promptBuilder.AppendLine($"- Date: {sourceItem.DateLostOrFound:yyyy-MM-dd}");
        promptBuilder.AppendLine($"- Description: {sourceItem.Description}");
        if (!string.IsNullOrEmpty(sourceItem.SecretIdentifier))
        {
            promptBuilder.AppendLine($"- Verification Question / Clue: {sourceItem.SecretIdentifier}");
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CANDIDATE OPPOSITE ITEMS TO COMPARE:");
        foreach (var c in candidates.Take(15))
        {
            promptBuilder.AppendLine($"Candidate #{c.Id}:");
            promptBuilder.AppendLine($"- Title: {c.Title}");
            promptBuilder.AppendLine($"- Category: {c.Category?.Name ?? "General"}");
            promptBuilder.AppendLine($"- Location: {c.Location}");
            promptBuilder.AppendLine($"- Date: {c.DateLostOrFound:yyyy-MM-dd}");
            promptBuilder.AppendLine($"- Description: {c.Description}");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("INSTRUCTIONS:");
        promptBuilder.AppendLine("1. Understand semantic synonyms (e.g. iPhone / smartphone, wallet / purse, black / dark grey).");
        promptBuilder.AppendLine("2. Consider physical descriptions, campus locations, dates, and item categories.");
        promptBuilder.AppendLine("3. Score each candidate between 0 and 100 representing probability of being the same item.");
        promptBuilder.AppendLine("4. Only include candidates with score >= 30.");
        promptBuilder.AppendLine("5. Output ONLY a valid JSON array in this exact schema:");
        promptBuilder.AppendLine("[ { \"candidateId\": <int>, \"score\": <int 0-100>, \"reason\": \"<1-2 sentence concise explanation>\" } ]");

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptBuilder.ToString() }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json"
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        string[] modelsToTry = [primaryModel, fallbackModel];
        string? responseJson = null;

        foreach (var model in modelsToTry)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseContent);
                    var candidatesProp = doc.RootElement.GetProperty("candidates");
                    if (candidatesProp.GetArrayLength() > 0)
                    {
                        var text = candidatesProp[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        responseJson = text;
                        break;
                    }
                }
                else
                {
                    _logger.LogWarning("Gemini API call to {Model} returned HTTP {StatusCode}", model, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call Gemini model {Model}", model);
            }
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return new List<MatchResult>();
        }

        var candidateMap = candidates.ToDictionary(c => c.Id);
        var matchDtos = JsonSerializer.Deserialize<List<GeminiMatchDto>>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<GeminiMatchDto>();

        var results = new List<MatchResult>();
        foreach (var dto in matchDtos)
        {
            if (candidateMap.TryGetValue(dto.CandidateId, out var cand) && dto.Score >= 30)
            {
                results.Add(new MatchResult
                {
                    CandidateItem = cand,
                    Score = Math.Clamp(dto.Score, 0, 100),
                    Reason = dto.Reason,
                    IsAiPowered = true
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToList();
    }

    private static List<MatchResult> MatchWithLocalHeuristic(Item sourceItem, List<Item> candidates, int limit)
    {
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
                reasons.Add("Category match");
            }

            // 2. Location Similarity (30%)
            var candLocationWords = ExtractWords(cand.Location);
            int locationOverlap = sourceLocationWords.Intersect(candLocationWords, StringComparer.OrdinalIgnoreCase).Count();
            if (locationOverlap > 0)
            {
                int locScore = Math.Min(30, locationOverlap * 15);
                score += locScore;
                reasons.Add($"Location proximity ({locationOverlap} shared term{(locationOverlap > 1 ? "s" : "")})");
            }

            // 3. Date Proximity (15%)
            int dayDiff = Math.Abs((cand.DateLostOrFound.Date - sourceItem.DateLostOrFound.Date).Days);
            if (dayDiff == 0)
            {
                score += 15;
                reasons.Add("Reported on the same date");
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
                    Reason = string.Join("; ", reasons),
                    IsAiPowered = false
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

    private class GeminiMatchDto
    {
        [JsonPropertyName("candidateId")]
        public int CandidateId { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
