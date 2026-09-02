namespace InfrastructureManager.Application.DTOs.Networks;

public class CreateNetworkDto
{
    public int    DepartmentId   { get; set; }  // LocationId derived from department
    public string Name           { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public string SubnetMask     { get; set; } = string.Empty;
    public int    Cidr           { get; set; }
    public string Gateway        { get; set; } = string.Empty;
    public string PrimaryDns     { get; set; } = string.Empty;
    public string? SecondaryDns  { get; set; }
    public string? DhcpRangeStart { get; set; }
    public string? DhcpRangeEnd   { get; set; }
    public bool   IsDhcpEnabled        { get; set; }
    public bool   IsInternetAccessible { get; set; }
    public int?   VlanId   { get; set; }
    public string? IspName { get; set; }
    public string? Notes   { get; set; }
}