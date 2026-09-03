using Microsoft.AspNetCore.Http;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IDeviceDocumentService
{
    Task<IEnumerable<DeviceDocumentDto>> GetByDeviceAsync(int deviceId);
    Task<IEnumerable<DocumentUploadResult>> UploadAsync(int deviceId, IFormFileCollection files, string? caption);
    Task DeleteAsync(int documentId);
    Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(int documentId);
}

public class DeviceDocumentDto
{
    public int      Id          { get; set; }
    public string   FileName    { get; set; } = string.Empty;
    public string   ContentType { get; set; } = string.Empty;
    public string?  Caption     { get; set; }
    public long     SizeBytes   { get; set; }
    public DateTime CreatedAt   { get; set; }
}

public class DocumentUploadResult
{
    public string  FileName { get; set; } = string.Empty;
    public bool    Success  { get; set; }
    public string? Error    { get; set; }
}