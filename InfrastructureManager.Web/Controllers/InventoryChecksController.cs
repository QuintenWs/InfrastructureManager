using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Web.ViewModels.InventoryChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

// Any authenticated user can perform a check — this is meant to be usable
// by other services verifying what's present, not just Admins.
[Authorize]
public class InventoryChecksController : Controller
{
    private readonly IInventoryCheckService _checkService;
    private readonly IDepartmentService     _departmentService;
    private readonly IDeviceService         _deviceService;
    private const int PageSize = 20;

    public InventoryChecksController(
        IInventoryCheckService checkService,
        IDepartmentService     departmentService,
        IDeviceService         deviceService)
    {
        _checkService      = checkService;
        _departmentService = departmentService;
        _deviceService     = deviceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId, int page = 1)
    {
        var departments = await GetDepartmentsAsync();

        var vm = new InventoryCheckIndexViewModel
        {
            Departments          = departments,
            SelectedDepartmentId = departmentId
        };

        if (departmentId.HasValue)
        {
            vm.DepartmentLabel = departments.FirstOrDefault(d => d.Value == departmentId.Value.ToString())?.Text;

            var paged = await _checkService.GetByDepartmentPagedAsync(departmentId.Value, page, PageSize);
            vm.Checks = paged.Items.ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = paged.Page,
                TotalPages  = paged.TotalPages,
                TotalCount  = paged.TotalCount,
                RouteValues = new Dictionary<string, string> { ["departmentId"] = departmentId.Value.ToString() }
            };
        }
        else
        {
            var paged = await _checkService.GetRecentPagedAsync(page, PageSize);
            vm.RecentChecks = paged.Items.ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = paged.Page,
                TotalPages  = paged.TotalPages,
                TotalCount  = paged.TotalCount,
                RouteValues = new Dictionary<string, string>()
            };
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int departmentId)
    {
        var dept = await _departmentService.GetByIdAsync(departmentId);
        if (dept == null) return NotFound();

        var devices = await _deviceService.FilterAsync(new DeviceFilter { DepartmentId = departmentId });

        var vm = new CreateInventoryCheckViewModel
        {
            DepartmentId   = departmentId,
            DepartmentName = dept.Name,
            LocationName   = dept.LocationName,
            Items = devices.OrderBy(d => d.Name).Select(d => new CheckItemRowViewModel
            {
                DeviceId   = d.Id,
                DeviceName = d.Name,
                DeviceType = d.DeviceType.ToString()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)] // several photos per submission
    public async Task<IActionResult> Create(CreateInventoryCheckViewModel vm)
    {
        var dto = new CreateInventoryCheckDto
        {
            DepartmentId = vm.DepartmentId,
            Notes        = vm.Notes,
            Items        = new List<CreateInventoryCheckItemDto>()
        };

        foreach (var row in vm.Items)
        {
            var item = new CreateInventoryCheckItemDto
            {
                DeviceId  = row.DeviceId,
                IsPresent = row.IsPresent,
                Remark    = row.Remark
            };

            if (row.Photo != null && row.Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await row.Photo.CopyToAsync(ms);
                item.PhotoData        = ms.ToArray();
                item.PhotoContentType = row.Photo.ContentType;
                item.PhotoFileName    = Path.GetFileName(row.Photo.FileName);
            }

            dto.Items.Add(item);
        }

        var id = await _checkService.CreateAsync(dto);

        TempData["Success"] = "Controle opgeslagen.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var check = await _checkService.GetByIdAsync(id);
        if (check == null) return NotFound();
        return View(check);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int itemId)
    {
        var result = await _checkService.GetPhotoAsync(itemId);
        if (result == null) return NotFound();
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    private async Task<List<SelectListItem>> GetDepartmentsAsync()
    {
        var items = await _departmentService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} – {x.LocationName}"
        }).ToList();
    }
}
