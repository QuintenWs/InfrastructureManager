using InfrastructureManager.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDepartmentDocumentService
{
    Task<IEnumerable<DepartmentDocumentDto>> GetByDepartmentAsync(int departmentId, DepartmentDocumentCategory? category = null);
    Task<IEnumerable<DocumentUploadResult>> UploadAsync(int departmentId, DepartmentDocumentCategory category, IFormFileCollection files, string? caption);
    Task DeleteAsync(int documentId);
    Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(int documentId);
}

public class DepartmentDocumentDto
{
    public int      Id          { get; set; }
    public string   FileName    { get; set; } = string.Empty;
    public string   ContentType { get; set; } = string.Empty;
    public string?  Caption     { get; set; }
    public long     SizeBytes   { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DepartmentDocumentCategory Category { get; set; }
}