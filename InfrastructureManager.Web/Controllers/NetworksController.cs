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


    public NetworksController(
        INetworkService    networkService,
        IDepartmentService departmentService,
        IUserAccessService userAccessService)
    {
        _networkService    = networkService;
        _departmentService = departmentService;
        _userAccessService = userAccessService;
    }


    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, bool? isDhcpEnabled,
        bool? isInternetAccessible, int? departmentId,
        int? vlanId, string? ispName, int page = 1)
    {
        var allowed = await _userAccessService.GetAccessibleLocationIdsAsync(User);
        var filter = new NetworkFilter
        {
            Search               = search,
            IsDhcpEnabled        = isDhcpEnabled,
            IsInternetAccessible = isInternetAccessible,
            DepartmentId         = departmentId,
            VlanId               = vlanId,
            IspName              = ispName
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
                VlanId               = vlanId,
                IspName              = ispName,
                Departments          = await GetDepartmentsAsync()
            }
        };

        ViewBag.Pagination = new PaginationViewModel
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
                ["vlanId"]               = vlanId?.ToString() ?? "",
                ["ispName"]              = ispName ?? ""
            }
        };

        return View(vm);
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
            Devices              = item.Devices
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(int? departmentId)
    {
        var vm = new CreateNetworkViewModel
        {
            DepartmentId = departmentId ?? 0,
            Departments  = await GetDepartmentsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateNetworkViewModel vm)
    {
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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _networkService.GetByIdAsync(id);
        if (item == null) return NotFound();

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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(UpdateNetworkViewModel vm)
    {
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
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _networkService.DeleteAsync(id);
        TempData["Success"] = "Network deleted.";
        return RedirectToAction(nameof(Index));
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
}
