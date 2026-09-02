namespace InfrastructureManager.Domain.Entities;

public class DepartmentPhoto
{
    public int    Id           { get; set; }
    public int    DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public string  FileName    { get; set; } = string.Empty;
    public byte[]  ImageData   { get; set; } = Array.Empty<byte>();
    public string  ContentType { get; set; } = "image/jpeg";
    public string? Caption     { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}
