using System.ComponentModel.DataAnnotations;

namespace InfrastructureManager.Web.ViewModels.Locations;

public class DepartmentInLocationViewModel
{
    public int    Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string Address      { get; set; } = string.Empty;
    public int    ContactCount { get; set; }
}

public class NetworkInLocationViewModel
{
    public int    Id             { get; set; }
    public string Name           { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public int    Cidr           { get; set; }
    public int    DeviceCount    { get; set; }
}

public class DeviceInLocationViewModel
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? IpAddress   { get; set; }   // nullable — lives in FieldValues now
    public string  DeviceType  { get; set; } = string.Empty;
    public string  Status      { get; set; } = string.Empty;
    public string? NetworkName { get; set; }
}