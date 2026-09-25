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
                await ValidateFileAsync(file);

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
                    newValues: new { Foto = photo.FileName, photo.Caption },
                    departmentId: departmentId);
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
            oldValues: new { Foto = photo.FileName, photo.Caption },
            departmentId: photo.DepartmentId);
    }

    public async Task<(byte[] Data, string ContentType, string FileName, int DepartmentId)?> GetPhotoAsync(int photoId)
    {
        var photo = await _context.DepartmentPhotos
            .AsNoTracking()
            .Select(p => new { p.Id, p.ImageData, p.ContentType, p.FileName, p.DepartmentId })
            .FirstOrDefaultAsync(p => p.Id == photoId);

        if (photo == null) return null;

        return (photo.ImageData, photo.ContentType, photo.FileName, photo.DepartmentId);
    }

    private static async Task ValidateFileAsync(IFormFile file)
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

        // Defense-in-depth: Content-Type hierboven komt rechtstreeks van de
        // client en kan vervalst worden. Deze check leest de eerste bytes van
        // het bestand zelf en vergelijkt met de bekende "magic numbers" van elk
        // toegestaan formaat, zodat een bestand dat zich enkel via zijn header
        // voordoet als een afbeelding (maar dat niet is) alsnog geweigerd wordt.
        if (!await LooksLikeAllowedImageAsync(file))
            throw new ArgumentException(
                $"'{file.FileName}' does not appear to be a valid image file.");
    }

    private static async Task<bool> LooksLikeAllowedImageAsync(IFormFile file)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        stream.Position = 0; // teruggezet zodat de effectieve upload verderop het volledige bestand nog kan lezen

        if (read < 4) return false;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (read >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) return true;

        // GIF: "GIF87a" of "GIF89a"
        if (read >= 6 &&
            header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 &&
            header[3] == 0x38 && (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61) return true;

        // WebP: "RIFF" .... "WEBP" (bytes 8-11)
        if (read >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;

        return false;
    }
}