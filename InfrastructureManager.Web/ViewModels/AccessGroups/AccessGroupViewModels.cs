using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.AccessGroups;

public class AccessGroupListViewModel
{
    public int     Id                   { get; set; }
    public string  Name                 { get; set; } = string.Empty;
    public string? Description          { get; set; }
    public bool    CanViewHistory       { get; set; }
    public int     DepartmentGrantCount { get; set; }
    public int     LocationGrantCount   { get; set; }
    public int     MemberCount          { get; set; }
}

public class AccessGrantRowViewModel
{
    public int    Id         { get; set; }
    public string Label      { get; set; } = string.Empty;
    public bool   IsLocation { get; set; }
}

public class AccessGroupMemberRowViewModel
{
    public string UserId      { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class AccessGroupDetailsViewModel
{
    public int     Id             { get; set; }
    public string  Name           { get; set; } = string.Empty;
    public string? Description    { get; set; }
    public bool    CanViewHistory { get; set; }

    public List<AccessGrantRowViewModel>       Grants  { get; set; } = new();
    public List<AccessGroupMemberRowViewModel> Members { get; set; } = new();

    public IEnumerable<SelectListItem> AvailableDepartments { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableLocations   { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AvailableUsers       { get; set; } = new List<SelectListItem>();
}

public class CreateAccessGroupViewModel
{
    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(200)]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Omschrijving")]
    public string? Description { get; set; }

    [Display(Name = "Mag geschiedenis bekijken")]
    public bool CanViewHistory { get; set; }
}

public class EditAccessGroupViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Naam is verplicht.")]
    [MaxLength(200)]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Omschrijving")]
    public string? Description { get; set; }

    [Display(Name = "Mag geschiedenis bekijken")]
    public bool CanViewHistory { get; set; }
}