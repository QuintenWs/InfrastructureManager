using System.ComponentModel.DataAnnotations;
using InfrastructureManager.Application.DTOs.AccessGroups;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.Users;

public class UserListViewModel
{
    public string Id          { get; set; } = string.Empty;
    public string FirstName   { get; set; } = string.Empty;
    public string LastName    { get; set; } = string.Empty;
    public string FullName    => $"{FirstName} {LastName}".Trim();
    public string Email       { get; set; } = string.Empty;
    public bool   IsActive    { get; set; }
    public string Role        { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>True voor Admin — ziet alles, ongeacht groepen/uitzonderingen.</summary>
    public bool IsUnrestricted             { get; set; }
    public int  AccessibleDepartmentCount  { get; set; }
}

public class CreateUserViewModel
{
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
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Rol")]
    public string Role { get; set; } = "Viewer";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Toegangsgroepen")]
    public List<int> AccessGroupIds { get; set; } = new();

    public IEnumerable<SelectListItem> AvailableGroups { get; set; } = new List<SelectListItem>();
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

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
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password (leave empty to keep current)")]
    public string? NewPassword { get; set; }

    [Required]
    [Display(Name = "Rol")]
    public string Role { get; set; } = "Viewer";

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Mag geschiedenis bekijken (individueel)")]
    public bool CanViewHistory { get; set; }

    [Display(Name = "Toegangsgroepen")]
    public List<int> AccessGroupIds { get; set; } = new();

    public IEnumerable<SelectListItem> AvailableGroups { get; set; } = new List<SelectListItem>();

    /// <summary>Enkel weergave — beheer gebeurt via de aparte Add/Remove-acties.</summary>
    public List<AccessGrantDto> IndividualGrants { get; set; } = new();

    public IEnumerable<SelectListItem> AvailableDepartments { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableLocations   { get; set; } = new List<SelectListItem>();
}