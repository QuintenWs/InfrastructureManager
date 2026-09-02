namespace InfrastructureManager.Application.DTOs.Locations;

public class LocationDto
{
    public int      Id              { get; set; }
    public string   Name            { get; set; } = string.Empty;
    public string   City            { get; set; } = string.Empty;
    public string   Country         { get; set; } = string.Empty;
    public string?  Notes           { get; set; }
    public DateTime CreatedAt       { get; set; }
    public int      DepartmentCount { get; set; }
    public int      NetworkCount    { get; set; }
    public int      DeviceCount     { get; set; }
    // Photos removed — photos now belong to Department
}