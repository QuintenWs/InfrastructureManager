using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Domain.Exceptions;
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
    private readonly IDeviceDocumentService  _deviceDocumentService;
    private readonly IUserAccessService      _userAccessService;
    private readonly IExportService          _exportService;

    public DevicesController(
        IDeviceService         deviceService,
        IDepartmentService     departmentService,
        INetworkService        networkService,
        IDeviceTypeService     deviceTypeService,
        IMaintenanceLogService maintenanceLogService,
        IDeviceDocumentService deviceDocumentService,
        IUserAccessService     userAccessService,
        IExportService         exportService)
    {
        _deviceService         = deviceService;
        _departmentService     = departmentService;
        _networkService        = networkService;
        _deviceTypeService     = deviceTypeService;
        _maintenanceLogService = maintenanceLogService;
        _deviceDocumentService = deviceDocumentService;
        _userAccessService     = userAccessService;
        _exportService         = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, int? deviceType,
        DeviceStatus? status, int? locationId, int? departmentId, int page = 1)
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var filter = new DeviceFilter
        {
            Search       = search,
            DeviceType   = deviceType.HasValue ? (DeviceType)deviceType.Value : null,
            Status       = status,
            LocationId   = locationId,
            DepartmentId = departmentId,
            AllowedDepartmentIds = allowed
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
                IpAddress      = x.IpAddress,
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
                Locations    = await GetLocationsAsync(),
                DeviceTypes  = await GetDeviceTypesAsync()
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
    public async Task<IActionResult> Export(
        string? search, int? deviceType,
        DeviceStatus? status, int? locationId, int? departmentId)
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var filter = new DeviceFilter
        {
            Search       = search,
            DeviceType   = deviceType.HasValue ? (DeviceType)deviceType.Value : null,
            Status       = status,
            LocationId   = locationId,
            DepartmentId = departmentId,
            AllowedDepartmentIds = allowed
        };

        var devices = await _deviceService.FilterAsync(filter);
        var bytes   = _exportService.ExportDevices(devices);
        var fileName = $"Devices_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _deviceService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanAccessDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var typeFields      = await _deviceTypeService.GetFieldsAsync(item.DeviceType, id);
        var maintenanceLogs = await _maintenanceLogService.GetByDeviceAsync(id);
        var documents       = await _deviceDocumentService.GetByDeviceAsync(id);

        var vm = new DeviceDetailsViewModel
        {
            Id = item.Id, DepartmentId = item.DepartmentId, DepartmentName = item.DepartmentName,
            LocationName = item.LocationName, NetworkName = item.NetworkName, Name = item.Name,
            DeviceType = item.DeviceType, Status = item.Status, Notes = item.Notes,
            TypeFields = typeFields?.Fields.Where(f => !string.IsNullOrWhiteSpace(f.CurrentValue)).ToList() ?? new(),
            MaintenanceLogs = maintenanceLogs.ToList(),
            Documents = documents.ToList(),
            CanEdit = await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId),
            CanViewHistory = await _userAccessService.CanViewHistoryAsync(User)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Document(int id)
    {
        var result = await _deviceDocumentService.GetAsync(id);
        if (result == null) return NotFound();
        var (data, contentType, fileName, departmentId) = result.Value;

        if (!await _userAccessService.CanAccessDepartmentAsync(User, departmentId))
            return RedirectToAction("AccessDenied", "Auth");

        return File(data, contentType, fileName);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadDocument(int deviceId, string? caption)
    {
        var device = await _deviceService.GetByIdAsync(deviceId);
        if (device == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, device.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "Selecteer minstens één document om te uploaden.";
            return RedirectToAction(nameof(Details), new { id = deviceId });
        }

        var results = await _deviceDocumentService.UploadAsync(deviceId, files, caption);
        var failed  = results.Where(r => !r.Success).ToList();
        TempData[failed.Any() ? "Error" : "Success"] = failed.Any()
            ? string.Join(", ", failed.Select(f => f.Error))
            : $"{results.Count(r => r.Success)} document(en) geüpload.";

        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> DeleteDocument(int documentId, int deviceId)
    {
        var actualDepartmentId = await _deviceDocumentService.GetDepartmentIdAsync(documentId);
        if (actualDepartmentId == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, actualDepartmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        await _deviceDocumentService.DeleteAsync(documentId);
        TempData["Success"] = "Document verwijderd.";
        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(int? departmentId, int? networkId)
    {
        if (departmentId.HasValue && !await _userAccessService.CanEditDepartmentAsync(User, departmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        var vm = new CreateDeviceViewModel
        {
            DepartmentId = departmentId ?? 0,
            NetworkId    = networkId,   // NIEUW — voorgeselecteerd bij aankomst via "Add Device" op Networks/Details
            Departments  = await GetDepartmentsAsync(),
            DeviceTypes  = await GetDeviceTypesAsync(),
            Status       = DeviceStatus.Active,
            DeviceType   = (int)DeviceType.Switch
        };

        if (departmentId.HasValue)
            vm.Networks = await GetNetworksForDepartmentAsync(departmentId.Value);

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(CreateDeviceViewModel vm)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            vm.DeviceTypes = await GetDeviceTypesAsync();
            vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
            return View(vm);
        }

        // Vóór het toestel zelf aan te maken: conflicteert een IP-veld met
        // een ander toestel op hetzelfde netwerk? Zo ja, hier al stoppen —
        // anders zou het toestel al aangemaakt zijn tegen de tijd dat
        // SaveFieldValuesAsync verderop het conflict zou detecteren, en zou
        // een herprobeer-poging een tweede, dubbel toestel aanmaken.
        if (vm.FieldValues?.Any() == true)
        {
            try
            {
                await _deviceTypeService.ValidateFieldValuesAsync(vm.NetworkId, excludeDeviceId: null, vm.FieldValues);
            }
            catch (Exception ex) when (ex is IpConflictException or DeviceFieldValidationException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.Departments = await GetDepartmentsAsync();
                vm.DeviceTypes = await GetDeviceTypesAsync();
                vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
                return View(vm);
            }
        }

        var deviceId = await _deviceService.CreateAsync(new CreateDeviceDto
        {
            DepartmentId = vm.DepartmentId,
            NetworkId    = vm.NetworkId,
            Name         = vm.Name,
            DeviceType   = (DeviceType)vm.DeviceType,
            Status       = vm.Status,
            Notes        = vm.Notes
        });

        if (vm.FieldValues?.Any() == true)
            await _deviceTypeService.SaveFieldValuesAsync(deviceId, vm.FieldValues);

        TempData["Success"] = "Device created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _deviceService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var typeFields = await _deviceTypeService.GetFieldsAsync(item.DeviceType, id);

        var vm = new UpdateDeviceViewModel
        {
            Id           = item.Id,
            Name         = item.Name,
            DepartmentId = item.DepartmentId,
            NetworkId    = item.NetworkId,
            DeviceType   = (int)item.DeviceType,
            Status       = item.Status,
            Notes        = item.Notes,
            Departments  = await GetDepartmentsAsync(),
            DeviceTypes  = await GetDeviceTypesAsync(),
            Networks     = await GetNetworksForDepartmentAsync(item.DepartmentId),
            TypeFields   = typeFields?.Fields.ToList() ?? new List<DeviceTypeFieldDto>()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(UpdateDeviceViewModel vm)
    {
        var original = await _deviceService.GetByIdAsync(vm.Id);
        if (original == null) return NotFound();

        if (!await _userAccessService.CanEditDepartmentAsync(User, original.DepartmentId) ||
            !await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            vm.DeviceTypes = await GetDeviceTypesAsync();
            vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
            vm.TypeFields  = (await _deviceTypeService.GetFieldsAsync((DeviceType)vm.DeviceType, vm.Id))
                             ?.Fields.ToList() ?? new List<DeviceTypeFieldDto>();
            return View(vm);
        }

        if (vm.FieldValues?.Any() == true)
        {
            try
            {
                await _deviceTypeService.ValidateFieldValuesAsync(vm.NetworkId, excludeDeviceId: vm.Id, vm.FieldValues);
            }
            catch (Exception ex) when (ex is IpConflictException or DeviceFieldValidationException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.Departments = await GetDepartmentsAsync();
                vm.DeviceTypes = await GetDeviceTypesAsync();
                vm.Networks    = await GetNetworksForDepartmentAsync(vm.DepartmentId);
                vm.TypeFields  = (await _deviceTypeService.GetFieldsAsync((DeviceType)vm.DeviceType, vm.Id))
                                 ?.Fields.ToList() ?? new List<DeviceTypeFieldDto>();
                return View(vm);
            }
        }

        await _deviceService.UpdateAsync(new UpdateDeviceDto
        {
            Id           = vm.Id,
            DepartmentId = vm.DepartmentId,
            NetworkId    = vm.NetworkId,
            Name         = vm.Name,
            DeviceType   = (DeviceType)vm.DeviceType,
            Status       = vm.Status,
            Notes        = vm.Notes
        });

        if (vm.FieldValues?.Any() == true)
            await _deviceTypeService.SaveFieldValuesAsync(vm.Id, vm.FieldValues);

        TempData["Success"] = "Device updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _deviceService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        await _deviceService.DeleteAsync(id);
        TempData["Success"] = "Device deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> AddMaintenanceLog(int deviceId, string note)
    {
        var device = await _deviceService.GetByIdAsync(deviceId);
        if (device == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, device.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

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
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> DeleteMaintenanceLog(int logId, int deviceId)
    {
        var actualDepartmentId = await _maintenanceLogService.GetDepartmentIdForLogAsync(logId);
        if (actualDepartmentId == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, actualDepartmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        await _maintenanceLogService.DeleteAsync(logId);
        TempData["Success"] = "Note deleted.";
        return RedirectToAction(nameof(Details), new { id = deviceId });
    }

    [HttpGet]
    public async Task<IActionResult> GetNetworksByDepartment(int departmentId)
    {
        if (!await _userAccessService.CanAccessDepartmentAsync(User, departmentId))
            return Forbid();

        var networks = await GetNetworksForDepartmentAsync(departmentId);
        return Json(networks.Select(n => new { value = n.Value, text = n.Text }));
    }

    [HttpGet]
    public async Task<IActionResult> GetTypeFields(int deviceType, int? deviceId)
    {
        if (deviceId.HasValue)
        {
            var device = await _deviceService.GetByIdAsync(deviceId.Value);
            if (device == null) return NotFound();
            if (!await _userAccessService.CanAccessDepartmentAsync(User, device.DepartmentId))
                return Forbid();
        }

        var result = await _deviceTypeService.GetFieldsAsync((DeviceType)deviceType, deviceId);
        return Json(result?.Fields ?? Enumerable.Empty<DeviceTypeFieldDto>());
    }

    private async Task<IEnumerable<SelectListItem>> GetDepartmentsAsync()
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var items = await _departmentService.GetAllAsync();
        if (allowed != null)
            items = items.Where(x => allowed.Contains(x.Id));

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
            new InfrastructureManager.Application.Filters.NetworkFilter { DepartmentId = departmentId });
        return networks.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} ({x.NetworkAddress}/{x.Cidr})"
        });
    }

    private async Task<IEnumerable<SelectListItem>> GetLocationsAsync()
    {
        var allowedLocationIds = await _userAccessService.GetAccessibleLocationIdsAsync(User);
        var departments = await _departmentService.GetAllAsync();
        var groups = departments
            .GroupBy(x => x.LocationName)
            .Select(g => new { LocationId = g.First().LocationId, LocationName = g.Key });

        if (allowedLocationIds != null)
            groups = groups.Where(g => allowedLocationIds.Contains(g.LocationId));

        return groups.Select(g => new SelectListItem
        {
            Value = g.LocationId.ToString(),
            Text  = g.LocationName
        });
    }

    private async Task<IEnumerable<SelectListItem>> GetDeviceTypesAsync()
    {
        var defs = await _deviceTypeService.GetAllDefinitionsAsync();
        return defs
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = ((int)d.DeviceType).ToString(),
                Text  = d.Name
            });
    }
}