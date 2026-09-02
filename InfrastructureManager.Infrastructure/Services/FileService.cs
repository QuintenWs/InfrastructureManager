using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly AppDbContext  _context;
    private readonly IAuditService _audit;

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly string[] AllowedTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    public FileService(AppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    public async Task<IEnumerable<PhotoUploadResult>> UploadDepartmentPhotosAsync(
        int                 departmentId,
        IFormFileCollection files,
        string?             sharedCaption)
    {
        var results = new List<PhotoUploadResult>();
        var added   = new List<DepartmentPhoto>();

        foreach (var file in files)
        {
            try
            {
                ValidateFile(file);

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var photo = new DepartmentPhoto
                {
                    DepartmentId = departmentId,
                    FileName     = Path.GetFileName(file.FileName),
                    ImageData    = ms.ToArray(),
                    ContentType  = file.ContentType.ToLower(),
                    Caption      = sharedCaption,
                    CreatedAt    = DateTime.UtcNow
                };
                _context.DepartmentPhotos.Add(photo);
                added.Add(photo);

                results.Add(new PhotoUploadResult { FileName = file.FileName, Success = true });
            }
            catch (ArgumentException ex)
            {
                results.Add(new PhotoUploadResult
                {
                    FileName = file.FileName, Success = false, Error = ex.Message
                });
            }
        }

        if (added.Count > 0)
        {
            await _context.SaveChangesAsync();

            var deptName = (await _context.Departments.FindAsync(departmentId))?.Name ?? $"Departement #{departmentId}";
            foreach (var photo in added)
            {
                await _audit.LogAsync("CREATE", "Department", departmentId, deptName,
                    newValues: new { Foto = photo.FileName, photo.Caption });
            }
        }

        return results;
    }

    public async Task DeleteDepartmentPhotoAsync(int photoId)
    {
        var photo = await _context.DepartmentPhotos.FindAsync(photoId);
        if (photo == null) return;

        _context.DepartmentPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        var deptName = (await _context.Departments.FindAsync(photo.DepartmentId))?.Name ?? $"Departement #{photo.DepartmentId}";
        await _audit.LogAsync("DELETE", "Department", photo.DepartmentId, deptName,
            oldValues: new { Foto = photo.FileName, photo.Caption });
    }

    public async Task<(byte[] Data, string ContentType, string FileName)?> GetPhotoAsync(int photoId)
    {
        var photo = await _context.DepartmentPhotos
            .AsNoTracking()
            .Select(p => new { p.Id, p.ImageData, p.ContentType, p.FileName })
            .FirstOrDefaultAsync(p => p.Id == photoId);

        if (photo == null) return null;

        return (photo.ImageData, photo.ContentType, photo.FileName);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException($"'{file.FileName}' is empty.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException($"'{file.FileName}' exceeds the maximum size of 10 MB.");

        if (!AllowedTypes.Contains(file.ContentType.ToLower()))
            throw new ArgumentException(
                $"'{file.FileName}' is not an allowed type. Only JPEG, PNG, GIF and WebP are accepted.");

        if (file.FileName.Length > 260)
            throw new ArgumentException("Filename too long.");
    }
}
