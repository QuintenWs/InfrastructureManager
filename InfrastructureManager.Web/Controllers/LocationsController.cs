using InfrastructureManager.Application.DTOs.Locations;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Locations;
using InfrastructureManager.Web.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class LocationsController : Controller
{
    private const int PageSize = 20;

    private readonly ILocationService _service;

    public LocationsController(ILocationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var paged = await _service.GetPagedAsync(search, page, PageSize);

        ViewBag.Search = search;
        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = paged.Page,
            TotalPages  = paged.TotalPages,
            TotalCount  = paged.TotalCount,
            RouteValues = new Dictionary<string, string> { ["search"] = search ?? "" }
        };

        return View(paged.Items.Select(x => new LocationListViewModel
        {
            Id              = x.Id,
            Name            = x.Name,
            City            = x.City,
            Country         = x.Country,
            DepartmentCount = x.DepartmentCount,
            NetworkCount    = x.NetworkCount,
            DeviceCount     = x.DeviceCount,
            CreatedAt       = x.CreatedAt
        }));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _service.GetDetailsByIdAsync(id);
        if (item == null) return NotFound();

        var vm = new LocationDetailsViewModel
        {
            Id          = item.Id,
            Name        = item.Name,
            City        = item.City,
            Country     = item.Country,
            Notes       = item.Notes,
            CreatedAt   = item.CreatedAt,
            Departments = item.Departments.Select(d => new DepartmentInLocationViewModel
            {
                Id           = d.Id,
                Name         = d.Name,
                Address      = d.Address,
                ContactCount = d.ContactCount
            }),
            Networks = item.Networks.Select(n => new NetworkInLocationViewModel
            {
                Id             = n.Id,
                Name           = n.Name,
                NetworkAddress = n.NetworkAddress,
                Cidr           = n.Cidr,
                DeviceCount    = n.DeviceCount
            }),
            Devices = item.Devices.Select(d => new DeviceInLocationViewModel
            {
                Id          = d.Id,
                Name        = d.Name,
                IpAddress   = d.IpAddress,
                DeviceType  = d.DeviceType.ToString(),
                Status      = d.Status.ToString(),
                NetworkName = d.NetworkName
            })
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create() => View(new CreateLocationViewModel());

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateLocationViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.CreateAsync(new CreateLocationDto
        {
            Name = vm.Name, City = vm.City, Country = vm.Country, Notes = vm.Notes
        });
        TempData["Success"] = $"Location '{vm.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(new UpdateLocationViewModel
        {
            Id = item.Id, Name = item.Name, City = item.City,
            Country = item.Country, Notes = item.Notes
        });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(UpdateLocationViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _service.UpdateAsync(new UpdateLocationDto
        {
            Id = vm.Id, Name = vm.Name, City = vm.City,
            Country = vm.Country, Notes = vm.Notes
        });
        TempData["Success"] = "Location updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        TempData["Success"] = "Location deleted.";
        return RedirectToAction(nameof(Index));
    }
}