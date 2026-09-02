namespace InfrastructureManager.Application.DTOs.Dashboard;

public class RecentDeviceDto
{
    public int Id { get; set; }

    public string Name { get; set; }
        = string.Empty;

    public string DeviceType { get; set; }
        = string.Empty;

    public string Status { get; set; }
        = string.Empty;

    public string LocationName { get; set; }
        = string.Empty;
}