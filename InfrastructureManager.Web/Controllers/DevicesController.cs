using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Devices;
using InfrastructureManager.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class DevicesController : Controller
{
    private const int PageSize = 20;

    private readonly IDeviceService          _deviceService;
    private readonly IDepartmentService      _departmentService;
    private readonly INetworkService         _networkService;
    private readonly IDeviceTypeService      _deviceTypeService;
    private readonly IMaintenanceLogService  _maintenanceLogService;

    public DevicesController(
        IDeviceService         deviceService,
        IDepartmentService     departmentService,
        INetworkService        networkService,
        IDeviceTypeService     deviceTypeService,
        IMaintenanceLogService maintenanceLogService,
        IDeviceDocumentService deviceDocumentService)
    {
        _deviceService         = deviceService;
        _departmentService     = departmentService;
        _networkService        = networkService;
        _deviceTypeService     = deviceTypeService;
        _maintenanceLogService = maintenanceLogService;
        _deviceDocumentService = deviceDocumentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, DeviceType? deviceType,
        DeviceStatus? status, int? locationId, int? departmentId, int page = 1)
    {
        var filter = new DeviceFilter
        {
            Search       = search,
            DeviceType   = deviceType,
            Status       = status,
            LocationId   = locationId,
            DepartmentId = departmentId
        };

        var paged = await _deviceService.FilterPagedAsync(filter, page, PageSize);

        var routeValues = new Dictionary<string, string>
        {
            ["search"]       = search ?? "",
            ["deviceType"]   = deviceType?.ToString() ?? "",
            ["status"]       = status?.ToString() ?? "",
            ["locationId"]   = locationId?.ToString() ?? "",
            ["departmentId"] = departmentId?.ToString() ?? ""
        };

        var vm = new DeviceIndexViewModel
        {
            Devices = paged.Items.Select(x => new DeviceListViewModel
            {
                Id             = x.Id,
                Name           = x.Name,
                IpAddress      = x.IpAddress, // convenience field from field values
                DeviceType     = x.DeviceType,
                Status         = x.Status,
                LocationName   = x.LocationName,
                DepartmentName = x.DepartmentName
            }),
            Filter = new DeviceFilterViewModel
            {
                Search       = search,
                DeviceType   = deviceType,
                Status       = status,
                LocationId   = locationId,
                DepartmentId = departmentId,
                Locations    = await GetLocationsAsync()
            },
            Pagination = new PaginationViewModel
            {
                CurrentPage = paged.Page,
                TotalPages  = paged.TotalPages,
                TotalCount  = paged.TotalCount,
                RouteValues = routeValues
            }
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _deviceService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccess.CanAccessDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var typeFields      = await _deviceTypeService.GetFieldsAsync(item.DeviceType, id);
        var maintenanceLogs = await _maintenanceLogService.GetByDeviceAsync(id);
        var documents       = await _documentService.GetByDeviceAsync(id);

        var vm = new DeviceDetailsViewModel
        {
            Id = item.Id, DepartmentId = item.DepartmentId, DepartmentName = item.DepartmentName,
            LocationName = item.LocationName, NetworkName = item.NetworkName, Name = item.Name,
            DeviceType = item.DeviceType, Status = item.Status, Notes = item.Notes,
            TypeFields = typeFields?.Fields.Where(f => !string.IsNullOrWhiteSpace(f.CurrentValue)).ToList() ?? new(),
            MaintenanceLogs = maintenanceLogs.ToList(),
            Documents = documents.ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Document(int id)
    {
        var result = await _documentService.GetAsync(id);
        if (result == null) return NotFound();
        var (data, contentType, fileName) = result.Value;
        return File(data, contentType, fileName);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadDocument(int deviceId, string? caption)
    {
        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "Selecteer minstens één document om te uploaden.";
            return RedirectToAction(nameof(Details), new { id = deviceId });
        }

        var results = await _documentService.UploadAsync(deviceId, files, caption);
        var failed  = results.Where(r => !r.Success).ToList();
        TempData[failed.Any() ? "Error" : "Success"] = failed.Any()
            ? string.Join(", ", failed.Select(f => f.Error))
            : $"{results.Count(r => r.Success)} document(en) geüpload.";

        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteDocument(int documentId, int deviceId)
    {
        await _documentService.DeleteAsync(documentId);
        TempData["Success"] = "Document verwijderd.";
        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(int? departmentId)
    {
        var vm = new CreateDeviceViewModel
        {
            DepartmentId = departmentId ?? 0,
            Departments  = await GetDepartmentsAsync(),
            Status       = DeviceStatus.Active,
            DeviceType   = DeviceType.Switch
        };

        if (departmentId.HasValue)
            vm.Networks = await GetNetworksForDepartmentAsync(departmentId.Value);

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateDeviceViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
            return View(vm);
        }

        var deviceId = await _deviceService.CreateAsync(new CreateDeviceDto
        {
            DepartmentId = vm.DepartmentId,
            NetworkId    = vm.NetworkId,
            Name         = vm.Name,
            DeviceType   = vm.DeviceType,
            Status       = vm.Status,
            Notes        = vm.Notes
        });

        if (vm.FieldValues?.Any() == true)
            await _deviceTypeService.SaveFieldValuesAsync(deviceId, vm.FieldValues);

        TempData["Success"] = "Device created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _deviceService.GetByIdAsync(id);
        if (item == null) return NotFound();

        var typeFields = await _deviceTypeService.GetFieldsAsync(item.DeviceType, id);

        var vm = new UpdateDeviceViewModel
        {
            Id           = item.Id,
            Name         = item.Name,
            DepartmentId = item.DepartmentId,
            NetworkId    = item.NetworkId,
            DeviceType   = item.DeviceType,
            Status       = item.Status,
            Notes        = item.Notes,
            Departments  = await GetDepartmentsAsync(),
            Networks     = await GetNetworksForDepartmentAsync(item.DepartmentId),
            TypeFields   = typeFields?.Fields.ToList() ?? new List<DeviceTypeFieldDto>()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(UpdateDeviceViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
            vm.TypeFields  = (await _deviceTypeService.GetFieldsAsync(vm.DeviceType, vm.Id))
                             ?.Fields.ToList() ?? new List<DeviceTypeFieldDto>();
            return View(vm);
        }

        await _deviceService.UpdateAsync(new UpdateDeviceDto
        {
            Id           = vm.Id,
            DepartmentId = vm.DepartmentId,
            NetworkId    = vm.NetworkId,
            Name         = vm.Name,
            DeviceType   = vm.DeviceType,
            Status       = vm.Status,
            Notes        = vm.Notes
        });

        if (vm.FieldValues?.Any() == true)
            await _deviceTypeService.SaveFieldValuesAsync(vm.Id, vm.FieldValues);

        TempData["Success"] = "Device updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _deviceService.DeleteAsync(id);
        TempData["Success"] = "Device deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddMaintenanceLog(int deviceId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            TempData["Error"] = "Note cannot be empty.";
            return RedirectToAction(nameof(Details), new { id = deviceId });
        }
        await _maintenanceLogService.AddAsync(deviceId, note);
        TempData["Success"] = "Maintenance note added.";
        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteMaintenanceLog(int logId, int deviceId)
    {
        await _maintenanceLogService.DeleteAsync(logId);
        TempData["Success"] = "Note deleted.";
        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpGet]
    public async Task<IActionResult> GetNetworksByDepartment(int departmentId)
    {
        var networks = await GetNetworksForDepartmentAsync(departmentId);
        return Json(networks.Select(n => new { value = n.Value, text = n.Text }));
    }

    [HttpGet]
    public async Task<IActionResult> GetTypeFields(DeviceType deviceType, int? deviceId)
    {
        var result = await _deviceTypeService.GetFieldsAsync(deviceType, deviceId);
        return Json(result?.Fields ?? Enumerable.Empty<DeviceTypeFieldDto>());
    }

    private async Task<IEnumerable<SelectListItem>> GetDepartmentsAsync()
    {
        var items = await _departmentService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} – {x.LocationName}"
        });
    }

    private async Task<IEnumerable<SelectListItem>> GetNetworksForDepartmentAsync(int departmentId)
    {
        if (departmentId <= 0) return Enumerable.Empty<SelectListItem>();
        var networks = await _networkService.FilterAsync(
            new NetworkFilter { DepartmentId = departmentId });
        return networks.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} ({x.NetworkAddress}/{x.Cidr})"
        });
    }

    private async Task<IEnumerable<SelectListItem>> GetLocationsAsync()
    {
        var departments = await _departmentService.GetAllAsync();
        return departments
            .GroupBy(x => x.LocationName)
            .Select(g => new SelectListItem
            {
                Value = g.First().LocationId.ToString(),
                Text  = g.Key
            });
    }
}