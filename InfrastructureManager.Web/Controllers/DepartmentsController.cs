using InfrastructureManager.Application.DTOs.Departments;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Departments;
using InfrastructureManager.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class DepartmentsController : Controller
{
    private const int PageSize = 20;

    private readonly IDepartmentService     _service;
    private readonly ILocationService       _locationService;
    private readonly IContactService        _contactService;
    private readonly IDeviceService         _deviceService;
    private readonly INetworkService        _networkService;
    private readonly IFileService           _fileService;
    private readonly IVisitService          _visitService;
    private readonly IInventoryCheckService _checkService;
    private readonly IUserAccessService     _userAccessService;

    public DepartmentsController(
        IDepartmentService     service,
        ILocationService       locationService,
        IContactService        contactService,
        IDeviceService         deviceService,
        INetworkService        networkService,
        IFileService           fileService,
        IVisitService          visitService,
        IInventoryCheckService checkService,
        IDepartmentDocumentService departmentDocumentService,
        IUserAccessService     userAccessService)
    {
        _service         = service;
        _locationService = locationService;
        _contactService  = contactService;
        _deviceService   = deviceService;
        _networkService  = networkService;
        _fileService     = fileService;
        _visitService    = visitService;
        _checkService    = checkService;
        _userAccessService  = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var allowed = await _userAccessService.GetAccessibleLocationIdsAsync(User);
        var paged      = await _service.GetPagedAsync(search, page, PageSize, allowed);
        var openCounts = await _visitService.GetOpenActionItemCountsAsync();

        ViewBag.Search = search;
        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = paged.Page,
            TotalPages  = paged.TotalPages,
            TotalCount  = paged.TotalCount,
            RouteValues = new Dictionary<string, string> { ["search"] = search ?? "" }
        };

        return View(paged.Items.Select(x => new DepartmentListViewModel
        {
            Id           = x.Id,
            Name         = x.Name,
            Description  = x.Description,
            LocationName = x.LocationName,
            Address      = x.Address,
            CreatedAt    = x.CreatedAt,
            OpenActionItemCount = openCounts.TryGetValue(x.Id, out var c) ? c : 0
        }));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!await _userAccessService.CanAccessDepartmentAsync(User, id))
        return RedirectToAction("AccessDenied", "Auth");

        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();

        var contacts            = await _contactService.GetByDepartmentAsync(id);
        var devices              = await _deviceService.FilterAsync(new DeviceFilter { DepartmentId = id });
        var networks             = await _networkService.FilterAsync(new NetworkFilter { DepartmentId = id });
        var photos               = await _service.GetPhotosAsync(id);
        var openActionItemCount  = await _visitService.GetOpenActionItemCountAsync(id);
        var lastCheck            = (await _checkService.GetByDepartmentAsync(id)).FirstOrDefault();

        var typeSummary = devices
            .GroupBy(d => d.DeviceType)
            .Select(g => new DeviceTypeSummaryViewModel
            {
                DeviceType = g.Key,
                Label      = g.Key.ToString(),
                Count      = g.Count()
            })
            .OrderBy(x => x.Label)
            .ToList();

        var vm = new DepartmentDetailsViewModel
        {
            Id           = item.Id,
            Name         = item.Name,
            Description  = item.Description,
            Address      = item.Address,
            Notes        = item.Notes,
            LocationId   = item.LocationId,
            LocationName = item.LocationName,
            CreatedAt    = item.CreatedAt,
            OpenActionItemCount = openActionItemCount,
            LastCheckDate       = lastCheck?.CheckDate,
            Contacts     = contacts.Select(c => new ContactInDeptViewModel
            {
                Id       = c.Id,
                FullName = c.FullName,
                Role     = c.Role,
                Email    = c.Email,
                Phone    = c.Phone
            }),
            DeviceTypeSummary = typeSummary,
            Networks = networks.Select(n => new NetworkInDeptViewModel
            {
                Id             = n.Id,
                Name           = n.Name,
                NetworkAddress = n.NetworkAddress,
                Cidr           = n.Cidr,
                Gateway        = n.Gateway,
                DeviceCount    = n.DeviceCount
            }),
            Photos = photos.Select(p => new DepartmentPhotoViewModel
            {
                Id      = p.Id,
                Caption = p.Caption
            })
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Report(int id)
    {
        if (!await _userAccessService.CanAccessDepartmentAsync(User, id))
        return RedirectToAction("AccessDenied", "Auth");

        var report = await _service.GetReportAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    /// <summary>Blank, printable checklist (presence checkbox + remarks column) for another
    /// service to walk through and fill in by hand — no data is stored from this action.</summary>
    [HttpGet]
    public async Task<IActionResult> ChecklistPrint(int id)
    {
        if (!await _userAccessService.CanAccessDepartmentAsync(User, id))
        return RedirectToAction("AccessDenied", "Auth");
        
        var report = await _service.GetReportAsync(id);
        if (report == null) return NotFound();
        return View(report);
    }

    // ── Photo endpoints ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Photo(int id)
    {
        var result = await _fileService.GetPhotoAsync(id);
        if (result == null) return NotFound();
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UploadPhoto(int departmentId, string? caption)
    {
        var files = Request.Form.Files;

        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "Please select at least one photo to upload.";
            return RedirectToAction(nameof(Details), new { id = departmentId });
        }

        var results = await _fileService.UploadDepartmentPhotosAsync(departmentId, files, caption);

        var failed = results.Where(r => !r.Success).ToList();
        if (failed.Any())
            TempData["Error"] = string.Join(", ", failed.Select(f => f.Error));
        else
            TempData["Success"] = $"{results.Count(r => r.Success)} photo(s) uploaded.";

        return RedirectToAction(nameof(Details), new { id = departmentId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeletePhoto(int photoId, int departmentId)
    {
        await _fileService.DeleteDepartmentPhotoAsync(photoId);
        TempData["Success"] = "Photo deleted.";
        return RedirectToAction(nameof(Details), new { id = departmentId });
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(int? locationId)
    {
        return View(new CreateDepartmentViewModel
        {
            LocationId = locationId ?? 0,
            Locations  = await GetLocationsAsync()
        });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateDepartmentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Locations = await GetLocationsAsync();
            return View(vm);
        }
        await _service.CreateAsync(new CreateDepartmentDto
        {
            LocationId  = vm.LocationId,
            Name        = vm.Name,
            Description = vm.Description,
            Address     = vm.Address,
            Notes       = vm.Notes
        });
        TempData["Success"] = $"Department '{vm.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(new UpdateDepartmentViewModel
        {
            Id          = item.Id,
            LocationId  = item.LocationId,
            Name        = item.Name,
            Description = item.Description,
            Address     = item.Address,
            Notes       = item.Notes,
            Locations   = await GetLocationsAsync()
        });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(UpdateDepartmentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Locations = await GetLocationsAsync();
            return View(vm);
        }
        await _service.UpdateAsync(new UpdateDepartmentDto
        {
            Id          = vm.Id,
            LocationId  = vm.LocationId,
            Name        = vm.Name,
            Description = vm.Description,
            Address     = vm.Address,
            Notes       = vm.Notes
        });
        TempData["Success"] = "Department updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Department deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetLocationsAsync()
    {
        var items = await _locationService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} ({x.City})"
        });
    }
}