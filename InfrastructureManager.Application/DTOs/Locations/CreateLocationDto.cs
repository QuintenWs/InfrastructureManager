namespace InfrastructureManager.Application.DTOs.Locations;

public class CreateLocationDto
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Notes { get; set; }
}