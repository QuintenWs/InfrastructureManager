using Microsoft.AspNetCore.Http;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IFileService
{
    Task<IEnumerable<PhotoUploadResult>> UploadDepartmentPhotosAsync(
        int                 departmentId,
        IFormFileCollection files,
        string?             sharedCaption);

    Task DeleteDepartmentPhotoAsync(int photoId);

    /// <summary>DepartmentId zit in de tuple zodat de controller toegang kan
    /// controleren vóór het bestand wordt teruggegeven.</summary>
    Task<(byte[] Data, string ContentType, string FileName, int DepartmentId)?> GetPhotoAsync(int photoId);
}

public class PhotoUploadResult
{
    public string  FileName { get; set; } = string.Empty;
    public bool    Success  { get; set; }
    public string? Error    { get; set; }
}