using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Web.ViewModels.Departments;

public class DepartmentDetailsViewModel
{
    public int     Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string  Address      { get; set; } = string.Empty;
    public string? Notes        { get; set; }
    public int     LocationId   { get; set; }
    public string  LocationName { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; }

    /// <summary>Open + in-progress action items for this department — see Visits.</summary>
    public int OpenActionItemCount { get; set; }

    /// <summary>Date of the most recent digital inventory check, if any — see Controles.</summary>
    public DateTime? LastCheckDate { get; set; }

    public IEnumerable<ContactInDeptViewModel>     Contacts          { get; set; } = new List<ContactInDeptViewModel>();
    public IEnumerable<DeviceTypeSummaryViewModel> DeviceTypeSummary { get; set; } = new List<DeviceTypeSummaryViewModel>();
    public IEnumerable<NetworkInDeptViewModel>     Networks          { get; set; } = new List<NetworkInDeptViewModel>();
    public IEnumerable<DepartmentPhotoViewModel>   Photos            { get; set; } = new List<DepartmentPhotoViewModel>();
}

public class DepartmentPhotoViewModel
{
    public int     Id      { get; set; }
    public string? Caption { get; set; }
}

public class ContactInDeptViewModel
{
    public int     Id       { get; set; }
    public string  FullName { get; set; } = string.Empty;
    public string? Role     { get; set; }
    public string  Email    { get; set; } = string.Empty;
    public string? Phone    { get; set; }
}

public class DeviceTypeSummaryViewModel
{
    public DeviceType DeviceType { get; set; }
    public string     Label      { get; set; } = string.Empty;
    public int        Count      { get; set; }
}

public class NetworkInDeptViewModel
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public int    Cidr           { get; set; }
    public string Gateway        { get; set; } = string.Empty;
    public int    DeviceCount    { get; set; }
}
