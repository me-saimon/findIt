using System.ComponentModel.DataAnnotations;

namespace FindIt.Models;

public enum ClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Disputed = 3
}

public class Claim
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }
    public virtual Item? Item { get; set; }

    [Required]
    public string ClaimantId { get; set; } = string.Empty;
    public virtual ApplicationUser? Claimant { get; set; }

    [Required]
    [Display(Name = "Answer to Secret Question / Identifying Details")]
    public string SecretAnswer { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Proof of Ownership Description")]
    public string ProofDescription { get; set; } = string.Empty;

    [Display(Name = "Proof Document / Photo")]
    public string? ProofImageUrl { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public virtual Dispute? Dispute { get; set; }
}
