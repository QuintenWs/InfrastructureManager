using Microsoft.AspNetCore.Http;

namespace InfrastructureManager.Application.Interfaces.Services;

public interface IFileService
{
    Task<IEnumerable<PhotoUploadResult>> UploadDepartmentPhotosAsync(
        int                 departmentId,
        IFormFileCollection files,
        string?             sharedCaption);

    Task DeleteDepartmentPhotoAsync(int photoId);

    Task<(byte[] Data, string ContentType, string FileName)?> GetPhotoAsync(int photoId);
}

public class PhotoUploadResult
{
    public string  FileName { get; set; } = string.Empty;
    public bool    Success  { get; set; }
    public string? Error    { get; set; }
}
