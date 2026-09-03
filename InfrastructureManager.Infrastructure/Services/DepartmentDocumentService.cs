using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class DepartmentDocumentService : IDepartmentDocumentService
{
    private readonly AppDbContext  _context;
    private readonly IAuditService _audit;

    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB
    private static readonly string[] AllowedCablingExtensions = { ".vsdx", ".vsd", ".pdf" };

    public DepartmentDocumentService(AppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    public async Task<IEnumerable<DepartmentDocumentDto>> GetByDepartmentAsync(int departmentId, DepartmentDocumentCategory? category = null)
    {
        var query = _context.DepartmentDocuments.Where(d => d.DepartmentId == departmentId);
        if (category.HasValue) query = query.Where(d => d.Category == category.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DepartmentDocumentDto
            {
                Id = d.Id, FileName = d.FileName, ContentType = d.ContentType,
                Caption = d.Caption, SizeBytes = d.FileData.Length, CreatedAt = d.CreatedAt, Category = d.Category
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentUploadResult>> UploadAsync(
        int departmentId, DepartmentDocumentCategory category, IFormFileCollection files, string? caption)
    {
        var results = new List<DocumentUploadResult>();
        var added   = new List<DepartmentDocument>();

        foreach (var file in files)
        {
            try
            {
                ValidateFile(file, category);
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);

                var doc = new DepartmentDocument
                {
                    DepartmentId = departmentId, Category = category,
                    FileName    = Path.GetFileName(file.FileName),
                    FileData    = ms.ToArray(),
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    Caption     = caption, CreatedAt = DateTime.UtcNow
                };
                _context.DepartmentDocuments.Add(doc);
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
            var deptName = (await _context.Departments.FindAsync(departmentId))?.Name ?? $"Departement #{departmentId}";
            foreach (var doc in added)
                await _audit.LogAsync("CREATE", "Department", departmentId, deptName,
                    newValues: new { Document = doc.FileName, doc.Category, doc.Caption });
        }

        return results;
    }

    public async Task DeleteAsync(int documentId)
    {
        var doc = await _context.DepartmentDocuments.FindAsync(documentId);
        if (doc == null) return;

        _context.DepartmentDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        var deptName = (await _context.Departments.FindAsync(doc.DepartmentId))?.Name ?? $"Departement #{doc.DepartmentId}";
        await _audit.LogAsync("DELETE", "Department", doc.DepartmentId, deptName,
            oldValues: new { Document = doc.FileName, doc.Category, doc.Caption });
    }

    public async Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(int documentId)
    {
        var doc = await _context.DepartmentDocuments
            .AsNoTracking()
            .Select(d => new { d.Id, d.FileData, d.ContentType, d.FileName })
            .FirstOrDefaultAsync(d => d.Id == documentId);

        return doc == null ? null : (doc.FileData, doc.ContentType, doc.FileName);
    }

    private static void ValidateFile(IFormFile file, DepartmentDocumentCategory category)
    {
        if (file.Length == 0) throw new ArgumentException($"'{file.FileName}' is leeg.");
        if (file.Length > MaxFileSizeBytes) throw new ArgumentException($"'{file.FileName}' overschrijdt de maximale grootte van 25 MB.");

        if (category == DepartmentDocumentCategory.CablingPlan)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedCablingExtensions.Contains(ext))
                throw new ArgumentException($"'{file.FileName}': voor een bekabelingsplan zijn enkel Visio (.vsdx/.vsd) of PDF toegelaten.");
        }
    }
}