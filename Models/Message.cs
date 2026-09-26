using System.ComponentModel.DataAnnotations;

namespace FindIt.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    public virtual ApplicationUser? Sender { get; set; }

    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    public virtual ApplicationUser? Receiver { get; set; }

    public int? ItemId { get; set; }
    public virtual Item? Item { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
