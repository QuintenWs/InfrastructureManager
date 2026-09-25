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
    private readonly IUserAccessService _userAccessService;
    private readonly IExportService          _exportService;
    private const int PageSize = 20;

    public ContactsController(
        IContactService    contactService,
        IDepartmentService departmentService,
        IUserAccessService userAccessService,
        IExportService         exportService)
    {
        _contactService    = contactService;
        _departmentService = departmentService;
        _userAccessService = userAccessService;
        _exportService         = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? departmentId, int page = 1)
    {
        if (departmentId.HasValue && !await _userAccessService.CanAccessDepartmentAsync(User, departmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var paged = await _contactService.GetPagedAsync(search, departmentId, page, PageSize, allowed);

        var vm = new ContactIndexViewModel
        {
            Items = paged.Items.Select(x => new ContactListViewModel
            {
                Id             = x.Id,
                FullName       = x.FullName,
                Email          = x.Email,
                Phone          = x.Phone,
                Role           = x.Role,
                DepartmentName = x.DepartmentName,
                LocationName   = x.LocationName
            }),
            Search       = search,
            DepartmentId = departmentId,
            Departments  = await GetDepartmentsAsync(),
            Pagination = new PaginationViewModel
            {
                CurrentPage = paged.Page,
                TotalPages  = paged.TotalPages,
                TotalCount  = paged.TotalCount,
                RouteValues = new Dictionary<string, string>
                {
                    ["search"]       = search ?? "",
                    ["departmentId"] = departmentId?.ToString() ?? ""
                }
            }
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? search, int? departmentId)
    {
        if (departmentId.HasValue && !await _userAccessService.CanAccessDepartmentAsync(User, departmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);

        // Hergebruikt GetPagedAsync met een zeer grote pageSize i.p.v. een
        // nieuwe "GetAllFiltered"-methode toe te voegen — IContactService.
        // GetAllAsync werd net in stap 5 als dode code verwijderd, en een derde
        // "alles ophalen"-variant zou dezelfde soort duplicatie herintroduceren
        // die dat opruimwerk net wegnam.
        var paged = await _contactService.GetPagedAsync(search, departmentId, page: 1, pageSize: int.MaxValue, allowed);
        var bytes = _exportService.ExportContacts(paged.Items);
        var fileName = $"Contacts_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _contactService.GetByIdAsync(id);
        if (item == null) return NotFound();

        if (!await _userAccessService.CanAccessDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

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
            CreatedAt      = item.CreatedAt,
            CanEdit        = await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId)
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(int? departmentId)
    {
        if (departmentId.HasValue && !await _userAccessService.CanEditDepartmentAsync(User, departmentId.Value))
            return RedirectToAction("AccessDenied", "Auth");

        var vm = new CreateContactViewModel
        {
            DepartmentId = departmentId ?? 0,
            Departments  = await GetDepartmentsAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(CreateContactViewModel vm)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

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
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _contactService.GetByIdAsync(id);
        if (item == null) return NotFound();

        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

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
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Edit(UpdateContactViewModel vm)
    {
        var original = await _contactService.GetByIdAsync(vm.Id);
        if (original == null) return NotFound();

        if (!await _userAccessService.CanEditDepartmentAsync(User, original.DepartmentId) ||
            !await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

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
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _contactService.GetByIdAsync(id);
        if (item == null) return NotFound();

        if (!await _userAccessService.CanEditDepartmentAsync(User, item.DepartmentId))
            return RedirectToAction("AccessDenied", "Auth");

        await _contactService.DeleteAsync(id);
        TempData["Success"] = "Contact deleted.";
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
}