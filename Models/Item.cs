using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FindIt.Models;

public enum ItemType
{
    Lost = 0,
    Found = 1
}

public enum ItemStatus
{
    Open = 0,
    Claimed = 1,
    Resolved = 2,
    Closed = 3
}

public class Item
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ItemType Type { get; set; } = ItemType.Lost;

    [Required]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date Lost / Found")]
    public DateTime DateLostOrFound { get; set; } = DateTime.Today;

    public string? ImageUrl { get; set; }

    /// <summary>
    /// For Found items: A hidden question or detail used to verify true owner
    /// (e.g., "What wallpaper is on the lockscreen?", "What brand is the keychain?")
    /// </summary>
    [Display(Name = "Secret Identifier (Ownership Proof Question)")]
    public string? SecretIdentifier { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Open;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
