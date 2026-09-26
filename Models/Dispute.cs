using System.ComponentModel.DataAnnotations;

namespace FindIt.Models;

public enum DisputeStatus
{
    Pending = 0,
    ResolvedInFavorOfClaimant = 1,
    ResolvedInFavorOfFinder = 2,
    Dismissed = 3
}

public class Dispute
{
    public int Id { get; set; }

    [Required]
    public int ClaimId { get; set; }
    public virtual Claim? Claim { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    public DisputeStatus Status { get; set; } = DisputeStatus.Pending;

    public string? ResolutionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
