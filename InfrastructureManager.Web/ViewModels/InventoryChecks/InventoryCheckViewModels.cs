using InfrastructureManager.Application.DTOs.InventoryChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.InventoryChecks;

public class InventoryCheckIndexViewModel
{
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    public int?    SelectedDepartmentId { get; set; }
    public string? DepartmentLabel      { get; set; }

    /// <summary>Populated when a department is selected.</summary>
    public List<InventoryCheckSummaryDto> Checks { get; set; } = new();

    /// <summary>Populated when no department is selected — recent checks across all departments.</summary>
    public List<InventoryCheckSummaryDto> RecentChecks { get; set; } = new();
}

public class CreateInventoryCheckViewModel
{
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName   { get; set; } = string.Empty;

    [Display(Name = "Algemene opmerkingen")]
    public string? Notes { get; set; }

    public List<CheckItemRowViewModel> Items { get; set; } = new();
}

public class CheckItemRowViewModel
{
    public int    DeviceId   { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;

    [Display(Name = "Aanwezig")]
    public bool IsPresent { get; set; } = true;

    [Display(Name = "Opmerking")]
    public string? Remark { get; set; }

    [Display(Name = "Foto")]
    public IFormFile? Photo { get; set; }
}
