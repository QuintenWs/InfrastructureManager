namespace InfrastructureManager.Web.ViewModels.Networks;

public class NetworkListViewModel
{
    public int    Id                   { get; set; }
    public string Name                 { get; set; } = string.Empty;
    public string DepartmentName       { get; set; } = string.Empty;
    public string LocationName         { get; set; } = string.Empty;
    public string NetworkAddress       { get; set; } = string.Empty;
    public int    Cidr                 { get; set; }
    public string Gateway              { get; set; } = string.Empty;
    public int?   VlanId               { get; set; }
    public bool   IsDhcpEnabled        { get; set; }
    public bool   IsInternetAccessible { get; set; }
    public int    DeviceCount          { get; set; }
}
