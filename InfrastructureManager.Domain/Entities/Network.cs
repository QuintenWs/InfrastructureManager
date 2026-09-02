namespace InfrastructureManager.Domain.Entities;

public class Network : BaseEntity
{
    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    /// <summary>Derived from Department.LocationId — kept for fast location queries.</summary>
    public int LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string NetworkAddress { get; set; } = string.Empty;

    public string SubnetMask { get; set; } = string.Empty;

    public int Cidr { get; set; }

    public string Gateway { get; set; } = string.Empty;

    public string PrimaryDns { get; set; } = string.Empty;

    public string SecondaryDns { get; set; } = string.Empty;

    public string DhcpRangeStart { get; set; } = string.Empty;

    public string DhcpRangeEnd { get; set; } = string.Empty;

    public bool IsDhcpEnabled { get; set; }

    public bool IsInternetAccessible { get; set; }

    public int? VlanId { get; set; }

    public string? IspName { get; set; }

    public string? Notes { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
