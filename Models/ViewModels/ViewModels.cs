using System.ComponentModel.DataAnnotations;
using FindIt.Models;

namespace FindIt.Models.ViewModels;

public class HomeViewModel
{
    public int TotalLost { get; set; }
    public int TotalFound { get; set; }
    public int TotalResolved { get; set; }
    public List<Item> RecentItems { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public class BrowseViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public ItemType? Type { get; set; }
    public ItemStatus? Status { get; set; }
    public string? SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public List<Item> Items { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public class ReportItemViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ItemType Type { get; set; } = ItemType.Lost;

    [Required(ErrorMessage = "Please select a category")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Location is required")]
    public string Location { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date Lost / Found")]
    public DateTime DateLostOrFound { get; set; } = DateTime.Today;

    [Display(Name = "Secret Identifier (Question/Proof Requirement)")]
    public string? SecretIdentifier { get; set; }

    [Display(Name = "Item Photo")]
    public IFormFile? ImageFile { get; set; }
}

public class ClaimSubmissionViewModel
{
    public int ItemId { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string ItemLocation { get; set; } = string.Empty;
    public string? SecretQuestionPrompt { get; set; }

    [Required(ErrorMessage = "Please answer the secret question or specify details")]
    [Display(Name = "Answer to Secret Question / Identifying Details")]
    public string SecretAnswer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide detailed proof of ownership")]
    [Display(Name = "Proof of Ownership Description")]
    public string ProofDescription { get; set; } = string.Empty;

    [Display(Name = "Proof Image or Receipt")]
    public IFormFile? ProofImageFile { get; set; }
}

public class DashboardViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public int MyReportCount { get; set; }
    public int MyClaimCount { get; set; }
    public int ResolvedCount { get; set; }
    public List<Item> MyRecentReports { get; set; } = new();
    public List<Claim> MyRecentClaims { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalItems { get; set; }
    public int PendingClaims { get; set; }
    public int ActiveDisputes { get; set; }
    public List<Item> RecentItems { get; set; } = new();
    public List<Claim> RecentClaims { get; set; } = new();
    public List<Dispute> Disputes { get; set; } = new();
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Department / Role")]
    public string? DepartmentOrRole { get; set; } = "Student";
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
