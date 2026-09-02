using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Departments;

public class UpdateDepartmentViewModel
{
    public int Id { get; set; }
 
    [Required(ErrorMessage = "Location is required.")]
    [Display(Name = "Location")]
    public int LocationId { get; set; }
 
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    [Display(Name = "Department Name")]
    public string Name { get; set; } = string.Empty;
 
    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
 
    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(300)]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;
 
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
 
    public IEnumerable<SelectListItem> Locations { get; set; }
        = new List<SelectListItem>();
}