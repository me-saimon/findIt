using Microsoft.AspNetCore.Identity;

namespace FindIt.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? DepartmentOrRole { get; set; } = "Student";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Item> ReportedItems { get; set; } = new List<Item>();
    public virtual ICollection<Claim> SubmittedClaims { get; set; } = new List<Claim>();
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
}
