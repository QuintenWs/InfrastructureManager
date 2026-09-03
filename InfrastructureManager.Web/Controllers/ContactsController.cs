using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class ContactsController : Controller
{
    private readonly IContactService    _contactService;
    private readonly IDepartmentService _departmentService;
    private const int PageSize = 20;

    public ContactsController(
        IContactService    contactService,
        IDepartmentService departmentService)
    {
        _contactService    = contactService;
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? departmentId, int page = 1)
    {
        var paged = await _contactService.GetPagedAsync(search, departmentId, page, PageSize);

        var vm = paged.Items.Select(x => new ContactListViewModel
        {
            Id             = x.Id,
            FullName       = x.FullName,
            Email          = x.Email,
            Phone          = x.Phone,
            Role           = x.Role,
            DepartmentName = x.DepartmentName,
            LocationName   = x.LocationName
        });

        ViewBag.Search       = search;
        ViewBag.DepartmentId = departmentId;
        ViewBag.Departments  = await GetDepartmentsAsync();
        ViewBag.Pagination   = new PaginationViewModel
        {
            CurrentPage = paged.Page,
            TotalPages  = paged.TotalPages,
            TotalCount  = paged.TotalCount,
            RouteValues = new Dictionary<string, string>
            {
                ["search"]       = search ?? "",
                ["departmentId"] = departmentId?.ToString() ?? ""
            }
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _contactService.GetByIdAsync(id);
        if (item == null) return NotFound();

        var vm = new ContactDetailsViewModel
        {
            Id             = item.Id,
            FirstName      = item.FirstName,
            LastName       = item.LastName,
            Email          = item.Email,
            Phone          = item.Phone,
            Role           = item.Role,
            Notes          = item.Notes,
            DepartmentId   = item.DepartmentId,
            DepartmentName = item.DepartmentName,
            LocationName   = item.LocationName,
            CreatedAt      = item.CreatedAt
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(int? departmentId)
    {
        var vm = new CreateContactViewModel
        {
            DepartmentId = departmentId ?? 0,
            Departments  = await GetDepartmentsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateContactViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }

        await _contactService.CreateAsync(new CreateContactDto
        {
            DepartmentId = vm.DepartmentId,
            FirstName    = vm.FirstName,
            LastName     = vm.LastName,
            Email        = vm.Email,
            Phone        = vm.Phone,
            Role         = vm.Role,
            Notes        = vm.Notes
        });

        TempData["Success"] = $"Contact '{vm.FirstName} {vm.LastName}' added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _contactService.GetByIdAsync(id);
        if (item == null) return NotFound();

        var vm = new UpdateContactViewModel
        {
            Id           = item.Id,
            DepartmentId = item.DepartmentId,
            FirstName    = item.FirstName,
            LastName     = item.LastName,
            Email        = item.Email,
            Phone        = item.Phone,
            Role         = item.Role,
            Notes        = item.Notes,
            Departments  = await GetDepartmentsAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(UpdateContactViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Departments = await GetDepartmentsAsync();
            return View(vm);
        }

        await _contactService.UpdateAsync(new UpdateContactDto
        {
            Id           = vm.Id,
            DepartmentId = vm.DepartmentId,
            FirstName    = vm.FirstName,
            LastName     = vm.LastName,
            Email        = vm.Email,
            Phone        = vm.Phone,
            Role         = vm.Role,
            Notes        = vm.Notes
        });

        TempData["Success"] = "Contact updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _contactService.DeleteAsync(id);
        TempData["Success"] = "Contact deleted.";
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
