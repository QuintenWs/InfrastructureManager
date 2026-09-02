namespace InfrastructureManager.Application.DTOs.Locations;

public class LocationPhotoDto
{
    public int     Id       { get; set; }
    public string  FileName { get; set; } = string.Empty;
    public string? Caption  { get; set; }
}
