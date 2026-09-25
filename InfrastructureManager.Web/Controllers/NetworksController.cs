using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Exceptions;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Networks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class NetworksController : Controller
{
    private readonly INetworkService    _networkService;
    private readonly IDepartmentService _departmentService;
    private const int PageSize = 20;
    private readonly IUserAccessService _userAccessService;
    private readonly IExportService          _exportService;

    public NetworksController(
        INetworkService    networkService,
        IDepartmentService departmentService,
        IUserAccessService userAccessService,
        IExportService         exportService)
    {
        _networkService    = networkService;
        _departmentService = departmentService;
        _userAccessService = userAccessService;
        _exportService         = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, bool? isDhcpEnabled,
        bool? isInternetAccessible, int? departmentId, int? locationId,
        int? vlanId, string? ispName, int page = 1)
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var filter = new NetworkFilter
        {
            Search               = search,
            IsDhcpEnabled        = isDhcpEnabled,
            IsInternetAccessible = isInternetAccessible,
            DepartmentId         = departmentId,
            LocationId           = locationId,
            VlanId               = vlanId,
            IspName              = ispName,
            AllowedDepartmentIds = allowed
        };

        var paged = await _networkService.FilterPagedAsync(filter, page, PageSize);

        var vm = new NetworkIndexViewModel
        {
            Networks = paged.Items.Select(x => new NetworkListViewModel
            {
                Id                   = x.Id,
                Name                 = x.Name,
                DepartmentName       = x.DepartmentName,
                LocationName         = x.LocationName,
                NetworkAddress       = x.NetworkAddress,
                Cidr                 = x.Cidr,
                Gateway              = x.Gateway,
                VlanId               = x.VlanId,
                IsDhcpEnabled        = x.IsDhcpEnabled,
                IsInternetAccessible = x.IsInternetAccessible,
                DeviceCount          = x.DeviceCount
            }),
            Filter = new NetworkFilterViewModel
            {
                Search               = search,
                IsDhcpEnabled        = isDhcpEnabled,
                IsInternetAccessible = isInternetAccessible,
                DepartmentId         = departmentId,
                LocationId           = locationId,
                VlanId               = vlanId,
                IspName              = ispName,
                Departments          = await GetDepartmentsAsync(),
                Locations            = await GetLocationsAsync()
            },
            Pagination = new PaginationViewModel
            {
                CurrentPage = paged.Page,
                TotalPages  = paged.TotalPages,
                TotalCount  = paged.TotalCount,
                RouteValues = new Dictionary<string, string>
                {
                    ["search"]               = search ?? "",
                    ["isDhcpEnabled"]        = isDhcpEnabled?.ToString() ?? "",
                    ["isInternetAccessible"] = isInternetAccessible?.ToString() ?? "",
                    ["departmentId"]         = departmentId?.ToString() ?? "",
                    ["locationId"]           = locationId?.ToString() ?? "",
                    ["vlanId"]               = vlanId?.ToString() ?? "",
                    ["ispName"]              = ispName ?? ""
                }
            }
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? search, bool? isDhcpEnabled,
        bool? isInternetAccessible, int? departmentId, int? locationId,
        int? vlanId, string? ispName)
    {
        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var filter = new NetworkFilter
        {
            Search               = search,
            IsDhcpEnabled        = isDhcpEnabled,
            IsInternetAccessible = isInternetAccessible,
            DepartmentId         = departmentId,
            LocationId           = locationId,
            VlanId               = vlanId,
            IspName              = ispName,
            AllowedDepartmentIds = allowed
        };

        var networks = await _networkService.FilterAsync(filter);
        var bytes    = _exportService.ExportNetworks(networks);
        var fileName = $"Networks_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _networkService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanAccessDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var vm = new NetworkDetailsViewModel
        {
            Id                   = item.Id,
            DepartmentId         = item.DepartmentId, 
            DepartmentName       = item.DepartmentName,
            LocationName         = item.LocationName,
            Name                 = item.Name,
            NetworkAddress       = item.NetworkAddress,
            SubnetMask           = item.SubnetMask,
            Cidr                 = item.Cidr,
            Gateway              = item.Gateway,
            PrimaryDns           = item.PrimaryDns,
            SecondaryDns         = item.SecondaryDns,
            DhcpRangeStart       = item.DhcpRangeStart,
            DhcpRangeEnd         = item.DhcpRangeEnd,
            IsDhcpEnabled        = item.IsDhcpEnabled,
            IsInternetAccessible = item.IsInternetAccessible,
            VlanId               = item.VlanId,
            IspName              = item.IspName,
            Notes                = item.Notes,
            DeviceCount          = item.DeviceCount,
            Devices              = item.Devices,
            CanEdit              = await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId)
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(int? departmentId)
    {
        if (departmentId.HasValue && !await _userAccessService.CanEditDepartmentAsync(User, departmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        var vm = new CreateNetworkViewModel
        {
            DepartmentId = departmentId ?? 0,
            Departments  = await GetDepartmentsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(CreateNetworkViewModel vm)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }

        try
        {
            await _networkService.CreateAsync(new CreateNetworkDto
            {
                DepartmentId         = vm.DepartmentId,
                Name                 = vm.Name,
                NetworkAddress       = vm.NetworkAddress,
                SubnetMask           = vm.SubnetMask,
                Cidr                 = vm.Cidr,
                Gateway              = vm.Gateway,
                PrimaryDns           = vm.PrimaryDns,
                SecondaryDns         = vm.SecondaryDns,
                DhcpRangeStart       = vm.DhcpRangeStart,
                DhcpRangeEnd         = vm.DhcpRangeEnd,
                IsDhcpEnabled        = vm.IsDhcpEnabled,
                IsInternetAccessible = vm.IsInternetAccessible,
                VlanId               = vm.VlanId,
                IspName              = vm.IspName,
                Notes                = vm.Notes
            });

            TempData["Success"] = "Network created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (SubnetValidationException ex)
        {
            ModelState.AddModelError(nameof(vm.NetworkAddress), ex.Message);
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _networkService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        var vm = new UpdateNetworkViewModel
        {
            Id                   = item.Id,
            DepartmentId         = item.DepartmentId,
            Name                 = item.Name,
            NetworkAddress       = item.NetworkAddress,
            SubnetMask           = item.SubnetMask,
            Cidr                 = item.Cidr,
            Gateway              = item.Gateway,
            PrimaryDns           = item.PrimaryDns,
            SecondaryDns         = item.SecondaryDns,
            DhcpRangeStart       = item.DhcpRangeStart,
            DhcpRangeEnd         = item.DhcpRangeEnd,
            IsDhcpEnabled        = item.IsDhcpEnabled,
            IsInternetAccessible = item.IsInternetAccessible,
            VlanId               = item.VlanId,
            IspName              = item.IspName,
            Notes                = item.Notes,
            Departments          = await GetDepartmentsAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(UpdateNetworkViewModel vm)
    {
        var original = await _networkService.GetByIdAsync(vm.Id);
        if (original == null) return NotFound();

        if (!await _userAccessService.CanEditDepartmentAsync(User, original.DepartmentId) ||
            !await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }

        try
        {
            await _networkService.UpdateAsync(new UpdateNetworkDto
            {
                Id                   = vm.Id,
                DepartmentId         = vm.DepartmentId,
                Name                 = vm.Name,
                NetworkAddress       = vm.NetworkAddress,
                SubnetMask           = vm.SubnetMask,
                Cidr                 = vm.Cidr,
                Gateway              = vm.Gateway,
                PrimaryDns           = vm.PrimaryDns,
                SecondaryDns         = vm.SecondaryDns,
                DhcpRangeStart       = vm.DhcpRangeStart,
                DhcpRangeEnd         = vm.DhcpRangeEnd,
                IsDhcpEnabled        = vm.IsDhcpEnabled,
                IsInternetAccessible = vm.IsInternetAccessible,
                VlanId               = vm.VlanId,
                IspName              = vm.IspName,
                Notes                = vm.Notes
            });

            TempData["Success"] = "Network updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (SubnetValidationException ex)
        {
            ModelState.AddModelError(nameof(vm.NetworkAddress), ex.Message);
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _networkService.GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        await _networkService.DeleteAsync(id);
        TempData["Success"] = "Network deleted.";
        return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> SuggestFreeIp(int id)
    {
        var network = await _networkService.GetByIdAsync(id);
        if (network == null) return NotFound();
        if (!await _userAccessService.CanAccessDepartmentAsync(User, network.DepartmentId))
            return Forbid();

        var ip = await _networkService.SuggestNextFreeIpAsync(id);
        return Json(new { ip });
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
}