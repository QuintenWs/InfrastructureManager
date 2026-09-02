using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Locations;

public class CreateLocationViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    [Display(Name = "Location Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [MaxLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    [MaxLength(100)]
    [Display(Name = "Country")]
    public string Country { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}