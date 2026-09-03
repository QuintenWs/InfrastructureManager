// Infrastructure/Services/DeviceDocumentService.cs
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DeviceDocumentService : IDeviceDocumentService
{
    private readonly AppDbContext  _context;
    private readonly IAuditService _audit;

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    // Enkel duidelijk gevaarlijke uitvoerbare bestanden blokkeren — alle
    // documenttypes (pdf, Office, tekst, archieven, afbeeldingen, ...) zijn toegelaten.
    private static readonly string[] BlockedExtensions =
        { ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".ps1", ".sh", ".scr", ".vbs", ".jar", ".jse", ".wsf" };

    public DeviceDocumentService(AppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    public async Task<IEnumerable<DeviceDocumentDto>> GetByDeviceAsync(int deviceId)
    {
        return await _context.DeviceDocuments
            .Where(d => d.DeviceId == deviceId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DeviceDocumentDto
            {
                Id = d.Id, FileName = d.FileName, ContentType = d.ContentType,
                Caption = d.Caption, SizeBytes = d.FileData.Length, CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentUploadResult>> UploadAsync(int deviceId, IFormFileCollection files, string? caption)
    {
        var results = new List<DocumentUploadResult>();
        var added   = new List<DeviceDocument>();

        foreach (var file in files)
        {
            try
            {
                ValidateFile(file);
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var doc = new DeviceDocument
                {
                    DeviceId    = deviceId,
                    FileName    = Path.GetFileName(file.FileName),
                    FileData    = ms.ToArray(),
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    Caption     = caption,
                    CreatedAt   = DateTime.UtcNow
                };
                _context.DeviceDocuments.Add(doc);
                added.Add(doc);
                results.Add(new DocumentUploadResult { FileName = file.FileName, Success = true });
            }
            catch (ArgumentException ex)
            {
                results.Add(new DocumentUploadResult { FileName = file.FileName, Success = false, Error = ex.Message });
            }
        }

        if (added.Count > 0)
        {
            await _context.SaveChangesAsync();
            var device = await _context.Devices.FindAsync(deviceId);
            foreach (var doc in added)
                await _audit.LogAsync("CREATE", "Device", deviceId, device?.Name ?? $"Toestel #{deviceId}",
                    newValues: new { Document = doc.FileName, doc.Caption });
        }

        return results;
    }

    public async Task DeleteAsync(int documentId)
    {
        var doc = await _context.DeviceDocuments.FindAsync(documentId);
        if (doc == null) return;

        _context.DeviceDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        var device = await _context.Devices.FindAsync(doc.DeviceId);
        await _audit.LogAsync("DELETE", "Device", doc.DeviceId, device?.Name ?? $"Toestel #{doc.DeviceId}",
            oldValues: new { Document = doc.FileName, doc.Caption });
    }

    public async Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(int documentId)
    {
        var doc = await _context.DeviceDocuments
            .AsNoTracking()
            .Select(d => new { d.Id, d.FileData, d.ContentType, d.FileName })
            .FirstOrDefaultAsync(d => d.Id == documentId);

        return doc == null ? null : (doc.FileData, doc.ContentType, doc.FileName);
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0) throw new ArgumentException($"'{file.FileName}' is leeg.");
        if (file.Length > MaxFileSizeBytes) throw new ArgumentException($"'{file.FileName}' overschrijdt de maximale grootte van 25 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (BlockedExtensions.Contains(ext))
            throw new ArgumentException($"'{file.FileName}': dit bestandstype is om veiligheidsredenen niet toegelaten.");

        if (file.FileName.Length > 260) throw new ArgumentException("Bestandsnaam is te lang.");
    }
}