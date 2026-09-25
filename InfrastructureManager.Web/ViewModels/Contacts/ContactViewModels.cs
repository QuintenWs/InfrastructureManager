using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using InfrastructureManager.Web.ViewModels.Shared; 

namespace InfrastructureManager.Web.ViewModels.Contacts;

public class ContactListViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
}

public class ContactIndexViewModel
{
    public IEnumerable<ContactListViewModel> Items { get; set; } = new List<ContactListViewModel>();
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    public PaginationViewModel Pagination { get; set; } = new();
}

public class ContactDetailsViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string? Notes { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>True als de ingelogde gebruiker dit contact mag bewerken
    /// (Admin, of Editor binnen zijn departement-scope).</summary>
    public bool CanEdit { get; set; }
}

public class CreateContactViewModel
{
    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(200)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Display(Name = "Role")]
    public string? Role { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}

public class UpdateContactViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Department is required.")]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(200)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Display(Name = "Role")]
    public string? Role { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
}