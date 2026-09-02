using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureManager.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ImportController : Controller
{
    private readonly IImportService   _importService;
    private readonly ITemplateService _templateService;

    public ImportController(
        IImportService   importService,
        ITemplateService templateService)
    {
        _importService   = importService;
        _templateService = templateService;
    }

    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Generates the Excel template dynamically based on the current
    /// device types and their fields, then serves it for download.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> DownloadTemplate()
    {
        var bytes = await _templateService.GenerateImportTemplateAsync();
        var fileName = $"ImportTemplate_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Upload(Microsoft.AspNetCore.Http.IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only .xlsx files are accepted.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _importService.ImportAsync(file);

        if (result.Success)
        {
            TempData["Success"] =
                $"Import successful — " +
                $"{result.LocationsAdded} location(s), " +
                $"{result.DepartmentsAdded} department(s), " +
                $"{result.NetworksAdded} network(s), " +
                $"{result.DevicesAdded} device(s) added.";
        }
        else
        {
            var where = result.ErrorSheet != null
                ? $" (sheet '{result.ErrorSheet}'" +
                  (result.ErrorRow.HasValue ? $", row {result.ErrorRow}" : "") + ")"
                : string.Empty;

            TempData["Error"] =
                $"Import cancelled — the entire file was rejected. " +
                $"Fix the error and try again{where}: {result.ErrorMessage}";
        }

        return RedirectToAction(nameof(Index));
    }
}