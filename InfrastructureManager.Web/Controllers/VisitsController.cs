using InfrastructureManager.Application.DTOs.Visits;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Web.ViewModels.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.Controllers;

// Any authenticated user (not just Admin) can log a visit — this is field
// work, not system administration. Worth revisiting once dedicated
// per-service roles exist.
[Authorize]
public class VisitsController : Controller
{
    private readonly IVisitService      _visitService;
    private readonly IDepartmentService _departmentService;

    public VisitsController(
        IVisitService      visitService,
        IDepartmentService departmentService)
    {
        _visitService      = visitService;
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId)
    {
        var departments = await GetDepartmentsAsync();

        var vm = new VisitIndexViewModel
        {
            Departments          = departments,
            SelectedDepartmentId = departmentId
        };

        if (departmentId.HasValue)
        {
            vm.DepartmentLabel = departments.FirstOrDefault(d => d.Value == departmentId.Value.ToString())?.Text;
            vm.Visits          = (await _visitService.GetVisitsByDepartmentAsync(departmentId.Value)).ToList();
            vm.OpenItems       = (await _visitService.GetOpenActionItemsByDepartmentAsync(departmentId.Value)).ToList();
        }
        else
        {
            vm.GlobalOpenItems = (await _visitService.GetAllOpenActionItemsAsync()).ToList();
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int departmentId)
    {
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
    public async Task<IActionResult> Create(CreateVisitViewModel vm)
    {
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
        return View(visit);
    }

    [HttpPost]
    public async Task<IActionResult> SetInProgress(int actionItemId, int? departmentId)
    {
        await _visitService.SetInProgressAsync(actionItemId);
        TempData["Success"] = "Actiepunt gemarkeerd als 'In behandeling'.";
        return departmentId.HasValue
            ? RedirectToAction(nameof(Index), new { departmentId })
            : RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetDepartmentsAsync()
    {
        var items = await _departmentService.GetAllAsync();
        return items.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text  = $"{x.Name} – {x.LocationName}"
        }).ToList();
    }
}
