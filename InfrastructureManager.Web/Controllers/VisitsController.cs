using InfrastructureManager.Application.DTOs.Visits;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

// Lezen (Index/Details) blijft toegankelijk voor elke gescoped gebruiker.
// Schrijven (Create/SetInProgress) is nu Admin+Editor, met een expliciete
// departement-scope-check — voorheen kon élke ingelogde gebruiker,
// inclusief een puur lezende Viewer, hier gewoon in schrijven.
[Authorize]
public class VisitsController : Controller
{
    private readonly IVisitService      _visitService;
    private readonly IDepartmentService _departmentService;
    private const int PageSize = 20;
    private readonly IUserAccessService  _userAccessService;

    public VisitsController(
        IVisitService      visitService,
        IDepartmentService departmentService,
        IUserAccessService userAccessService)
    {
        _visitService      = visitService;
        _departmentService = departmentService;
        _userAccessService       = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId, int page = 1)
    {
        if (departmentId.HasValue && !await _userAccessService.CanAccessDepartmentAsync(User, departmentId.Value))
        return RedirectToAction("AccessDenied", "Auth");

        var departments = await GetDepartmentsAsync();

        var vm = new VisitIndexViewModel
        {
            Departments          = departments,
            SelectedDepartmentId = departmentId
        };

        if (departmentId.HasValue)
        {
            vm.DepartmentLabel = departments.FirstOrDefault(d => d.Value == departmentId.Value.ToString())?.Text;

            var pagedVisits = await _visitService.GetVisitsByDepartmentPagedAsync(departmentId.Value, page, PageSize);
            vm.Visits    = pagedVisits.Items.ToList();
            vm.OpenItems = (await _visitService.GetOpenActionItemsByDepartmentAsync(departmentId.Value)).ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = pagedVisits.Page,
                TotalPages  = pagedVisits.TotalPages,
                TotalCount  = pagedVisits.TotalCount,
                RouteValues = new Dictionary<string, string> { ["departmentId"] = departmentId.Value.ToString() }
            };
        }
        else
        {
            // Ontbrak hier voordien: het globale overzicht (geen departement
            // gekozen) toonde open actiepunten over ALLE departementen heen.
            var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
            var pagedItems = await _visitService.GetAllOpenActionItemsPagedAsync(page, PageSize, allowedDepartmentIds: allowed);
            vm.GlobalOpenItems = pagedItems.Items.ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = pagedItems.Page,
                TotalPages  = pagedItems.TotalPages,
                TotalCount  = pagedItems.TotalCount,
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

        var openItems = await _visitService.GetOpenActionItemsByDepartmentAsync(departmentId);

        var vm = new CreateVisitViewModel
        {
            DepartmentId   = departmentId,
            DepartmentName = dept.Name,
            LocationName   = dept.LocationName,
            OpenItems = openItems.Select(i => new OpenItemRowViewModel
            {
                ActionItemId         = i.Id,
                Description           = i.Description,
                Priority              = i.Priority,
                CreatedAt             = i.CreatedAt,
                CreatedByDisplayName  = i.CreatedByDisplayName
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> Create(CreateVisitViewModel vm)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, vm.DepartmentId))
        return RedirectToAction("AccessDenied", "Auth");

        var dto = new CreateSiteVisitDto
        {
            DepartmentId  = vm.DepartmentId,
            Summary       = vm.Summary,
            ResolvedItems = vm.OpenItems
                .Where(i => i.Resolve)
                .Select(i => new ResolvedItemInput
                {
                    ActionItemId    = i.ActionItemId,
                    ResolutionNotes = i.ResolutionNotes
                })
                .ToList(),
            NewItems = vm.NewItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => new NewItemInput
                {
                    Description = i.Description!.Trim(),
                    Priority    = i.Priority
                })
                .ToList()
        };

        await _visitService.CreateVisitAsync(dto);

        TempData["Success"] = "Bezoek geregistreerd.";
        return RedirectToAction(nameof(Index), new { departmentId = vm.DepartmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var visit = await _visitService.GetVisitByIdAsync(id);
        if (visit == null) return NotFound();

        if (!await _userAccessService.CanAccessDepartmentAsync(User, visit.DepartmentId))
        return RedirectToAction("AccessDenied", "Auth");

        return View(visit);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> SetInProgress(int actionItemId, int? departmentId)
    {
        var actualDepartmentId = await _visitService.GetActionItemDepartmentIdAsync(actionItemId);
        if (actualDepartmentId == null) return NotFound();
        if (!await _userAccessService.CanEditDepartmentAsync(User, actualDepartmentId.Value))
        return RedirectToAction("AccessDenied", "Auth");

        await _visitService.SetInProgressAsync(actionItemId);
        TempData["Success"] = "Actiepunt gemarkeerd als 'In behandeling'.";
        return departmentId.HasValue
            ? RedirectToAction(nameof(Index), new { departmentId })
            : RedirectToAction(nameof(Index));
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