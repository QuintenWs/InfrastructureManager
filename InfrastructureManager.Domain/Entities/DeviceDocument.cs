namespace InfrastructureManager.Domain.Entities;

public class DeviceDocument
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public string  FileName    { get; set; } = string.Empty;
    public byte[]  FileData    { get; set; } = Array.Empty<byte>();
    public string  ContentType { get; set; } = "application/octet-stream";
    public string? Caption     { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}