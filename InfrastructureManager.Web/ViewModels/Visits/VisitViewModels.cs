using InfrastructureManager.Application.DTOs.Visits;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Visits;

public class VisitIndexViewModel
{
    public IEnumerable<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
    public int?    SelectedDepartmentId { get; set; }
    public string? DepartmentLabel      { get; set; }

    // Populated when a department is selected
    public List<SiteVisitDto>  Visits    { get; set; } = new();
    public List<ActionItemDto> OpenItems { get; set; } = new();

    // Populated when no department is selected — overview across all departments
    public List<ActionItemDto> GlobalOpenItems { get; set; } = new();
}

public class CreateVisitViewModel
{
    public int    DepartmentId   { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string LocationName   { get; set; } = string.Empty;

    [Display(Name = "Algemene opmerkingen")]
    public string? Summary { get; set; }

    public List<OpenItemRowViewModel> OpenItems { get; set; } = new();
    public List<NewItemRowViewModel>  NewItems  { get; set; } = new() { new NewItemRowViewModel() };
}

public class OpenItemRowViewModel
{
    public int      ActionItemId         { get; set; }
    public string   Description          { get; set; } = string.Empty;
    public string   Priority             { get; set; } = string.Empty;
    public DateTime CreatedAt            { get; set; }
    public string   CreatedByDisplayName { get; set; } = string.Empty;

    [Display(Name = "Opgelost")]
    public bool Resolve { get; set; }

    [Display(Name = "Opmerking bij oplossing")]
    public string? ResolutionNotes { get; set; }
}

public class NewItemRowViewModel
{
    [Display(Name = "Omschrijving")]
    public string? Description { get; set; }

    [Display(Name = "Prioriteit")]
    public string Priority { get; set; } = "Normal";
}
