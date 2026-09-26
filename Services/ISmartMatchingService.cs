using FindIt.Models;

namespace FindIt.Services;

public class MatchResult
{
    public Item CandidateItem { get; set; } = null!;
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public interface ISmartMatchingService
{
    Task<List<MatchResult>> FindMatchesAsync(Item item, int limit = 5);
}
