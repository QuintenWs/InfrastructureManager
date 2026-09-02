namespace InfrastructureManager.Application.DTOs.Departments;

public class DepartmentReportDto
{
    public int     Id           { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Address      { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public string? Notes        { get; set; }
    public string  LocationName { get; set; } = string.Empty;
    public string  LocationCity { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public IEnumerable<ContactReportDto>  Contacts  { get; set; } = new List<ContactReportDto>();
    public IEnumerable<NetworkReportDto>  Networks  { get; set; } = new List<NetworkReportDto>();
    public IEnumerable<DeviceReportDto>   Devices   { get; set; } = new List<DeviceReportDto>();
}

public class ContactReportDto
{
    public string  FullName { get; set; } = string.Empty;
    public string? Role     { get; set; }
    public string  Email    { get; set; } = string.Empty;
    public string? Phone    { get; set; }
}

public class NetworkReportDto
{
    public string  Name                 { get; set; } = string.Empty;
    public string  NetworkAddress       { get; set; } = string.Empty;
    public int     Cidr                 { get; set; }
    public string  SubnetMask           { get; set; } = string.Empty;
    public string  Gateway              { get; set; } = string.Empty;
    public string  PrimaryDns           { get; set; } = string.Empty;
    public string? SecondaryDns         { get; set; }
    public bool    IsDhcpEnabled        { get; set; }
    public string? DhcpRange            { get; set; }
    public bool    IsInternetAccessible { get; set; }
    public int?    VlanId               { get; set; }
    public string? IspName              { get; set; }
    public int     DeviceCount          { get; set; }
}

public class DeviceReportDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string  DeviceType  { get; set; } = string.Empty;
    public string  Status      { get; set; } = string.Empty;
    public string? NetworkName { get; set; }
    public string? Notes       { get; set; }

    // All type-specific properties come from custom fields
    public IEnumerable<(string Label, string Value)> CustomFields { get; set; }
        = new List<(string, string)>();
}