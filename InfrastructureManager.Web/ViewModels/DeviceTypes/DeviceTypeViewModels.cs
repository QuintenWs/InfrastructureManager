using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.DeviceTypes;

public class DeviceTypeListViewModel
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int    FieldCount  { get; set; }
    public int    DeviceCount { get; set; }
}

public class DeviceTypeDetailsViewModel
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int    DeviceCount { get; set; }
    public IEnumerable<DeviceTypeFieldViewModel> Fields { get; set; }
        = new List<DeviceTypeFieldViewModel>();

    // Inline add-field form
    public AddFieldViewModel NewField { get; set; } = new();
}

public class DeviceTypeFieldViewModel
{
    public int     Id            { get; set; }
    public string  Label         { get; set; } = string.Empty;
    public string  FieldKey      { get; set; } = string.Empty;
    public string  FieldType     { get; set; } = "text";
    public string? SelectOptions { get; set; }
    public bool    IsRequired    { get; set; }
    public bool    AlertOnExpiry { get; set; }
    public int     SortOrder     { get; set; }
}

public class CreateDeviceTypeViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public class EditDeviceTypeViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100)]
    [Display(Name = "Type Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public class AddFieldViewModel
{
    public int DefinitionId { get; set; }

    [Required(ErrorMessage = "Label is required.")]
    [MaxLength(100)]
    [Display(Name = "Field Label")]
    public string Label { get; set; } = string.Empty;

    [Display(Name = "Field Type")]
    public string FieldType { get; set; } = "text";

    [Display(Name = "Options (comma-separated, for select fields)")]
    public string? SelectOptions { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Waarschuw bij vervaldatum (enkel voor datumvelden)")]
    public bool AlertOnExpiry { get; set; }
}

public class EditFieldViewModel
{
    public int    FieldId       { get; set; }
    public int    DefinitionId  { get; set; }

    [Required(ErrorMessage = "Label is required.")]
    [MaxLength(100)]
    [Display(Name = "Field Label")]
    public string Label { get; set; } = string.Empty;

    [Display(Name = "Field Type")]
    public string FieldType { get; set; } = "text";

    [Display(Name = "Options (comma-separated, for select fields)")]
    public string? SelectOptions { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Waarschuw bij vervaldatum (enkel voor datumvelden)")]
    public bool AlertOnExpiry { get; set; }
}