using System.ComponentModel.DataAnnotations;

namespace FindIt.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? IconSvg { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
