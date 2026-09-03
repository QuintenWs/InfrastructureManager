namespace InfrastructureManager.Domain.Entities;

public class Department : BaseEntity
{
    public int    LocationId  { get; set; }
    public Location Location  { get; set; } = null!;

    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  Address     { get; set; } = string.Empty;
    public string? Notes       { get; set; }

    public ICollection<Contact>         Contacts { get; set; } = new List<Contact>();
    public ICollection<Device>          Devices  { get; set; } = new List<Device>();
    public ICollection<Network>         Networks { get; set; } = new List<Network>();
    public ICollection<DepartmentPhoto> Photos   { get; set; } = new List<DepartmentPhoto>();

    public ICollection<SiteVisit>       Visits          { get; set; } = new List<SiteVisit>();
    public ICollection<ActionItem>      ActionItems     { get; set; } = new List<ActionItem>();
    public ICollection<InventoryCheck>  InventoryChecks { get; set; } = new List<InventoryCheck>();

    public ICollection<DepartmentDocument> Documents { get; set; } = new List<DepartmentDocument>();
}
