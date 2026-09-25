using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.InventoryChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

// Lezen blijft toegankelijk voor elke gescoped gebruiker. Een controle
// uitvoeren (Create) is nu Admin+Editor — voorheen kon élke ingelogde
// gebruiker, ook een puur lezende Viewer, hier schrijven.
[Authorize]
public class InventoryChecksController : Controller
{
    private readonly IInventoryCheckService _checkService;
    private readonly IDepartmentService     _departmentService;
    private readonly IDeviceService         _deviceService;
    private const int PageSize = 20;
    private readonly IUserAccessService     _userAccessService;


    public InventoryChecksController(
        IInventoryCheckService checkService,
        IDepartmentService     departmentService,
        IDeviceService         deviceService,
        IUserAccessService     userAccessService)
    {
        _checkService      = checkService;
        _departmentService = departmentService;
        _deviceService     = deviceService;
        _userAccessService      = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId, int page = 1)
    {
        if (departmentId.HasValue && !await _userAccessService.CanAccessDepartmentAsync(User, departmentId.Value))
        return RedirectToAction("AccessDenied", "Auth");

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
            // Ontbrak hier voordien: het globale "recente controles"-overzicht
            // toonde controles van ALLE departementen, ongeacht restrictie.
            var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
            var paged = await _checkService.GetRecentPagedAsync(page, PageSize, allowed);
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
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(int departmentId)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, departmentId))
        return RedirectToAction("AccessDenied", "Auth");

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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, int? departmentId)
    {
        var check = await _checkService.GetByIdAsync(id);
        if (check == null) return NotFound();

        await _checkService.DeleteAsync(id);
        TempData["Success"] = "Check deleted.";
        return departmentId.HasValue
            ? RedirectToAction(nameof(Index), new { departmentId })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    [RequestSizeLimit(50_000_000)] // several photos per submission
    public async Task<IActionResult> Create(CreateInventoryCheckViewModel vm)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
        return RedirectToAction("AccessDenied", "Auth");

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

        TempData["Success"] = "Check saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var check = await _checkService.GetByIdAsync(id);
        if (check == null) return NotFound();

        if (!await _userAccessService.CanAccessDepartmentAsync(User, check.DepartmentId))
        return RedirectToAction("AccessDenied", "Auth");

        return View(check);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(int itemId)
    {
        var actualDepartmentId = await _checkService.GetDepartmentIdForItemAsync(itemId);
        if (actualDepartmentId == null) return NotFound();
        if (!await _userAccessService.CanAccessDepartmentAsync(User, actualDepartmentId.Value))
        return RedirectToAction("AccessDenied", "Auth");

        var result = await _checkService.GetPhotoAsync(itemId);
        if (result == null) return NotFound();
        var (data, contentType, fileName, _) = result.Value;
        return File(data, contentType, fileName);
    }

    private async Task<List<SelectListItem>> GetDepartmentsAsync()
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var items = await _departmentService.GetAllAsync();
        if (allowed != null)
            items = items.Where(x => allowed.Contains(x.Id));

        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} – {x.LocationName}"
        }).ToList();
    }
}