using InfrastructureManager.Domain.Enums;

namespace InfrastructureManager.Domain.Entities;

public class DepartmentDocument
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public DepartmentDocumentCategory Category { get; set; } = DepartmentDocumentCategory.Other;

    public string  FileName    { get; set; } = string.Empty;
    public byte[]  FileData    { get; set; } = Array.Empty<byte>();
    public string  ContentType { get; set; } = "application/octet-stream";
    public string? Caption     { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}