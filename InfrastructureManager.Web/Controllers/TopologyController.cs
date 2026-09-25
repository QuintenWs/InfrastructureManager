using InfrastructureManager.Application.DTOs.Topology;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.Topology;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Infrastructure.Data;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class TopologyController : Controller
{
    private readonly ITopologyService    _topologyService;
    private readonly AppDbContext        _context;
    private readonly IUserAccessService  _userAccessService;

    public TopologyController(ITopologyService topologyService, AppDbContext context, IUserAccessService userAccessService)
    {
        _topologyService = topologyService;
        _context         = context;
        _userAccessService       = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId)
    {
        if (departmentId.HasValue && !await _userAccessService.CanAccessDepartmentAsync(User, departmentId.Value))
        return RedirectToAction("AccessDenied", "Auth");

        var allowed = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var deptQuery = _context.Departments.Include(d => d.Location).AsQueryable();
        if (allowed != null)
            deptQuery = deptQuery.Where(d => allowed.Contains(d.Id));

        var departments = await deptQuery
            .OrderBy(d => d.Location.Name).ThenBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text  = $"{d.Name} — {d.Location.Name}"
            })
            .ToListAsync();

        var vm = new TopologyIndexViewModel
        {
            Departments  = departments,
            DepartmentId = departmentId,
            CanEdit      = departmentId.HasValue &&
                await _userAccessService.CanEditDepartmentAsync(User, departmentId.Value)
        };

        if (departmentId.HasValue)
            vm.Topology = await _topologyService.GetByDepartmentAsync(departmentId.Value);

        return View(vm);
    }

    /// <summary>Returns topology as JSON for the JS renderer.</summary>
    [HttpGet]
    public async Task<IActionResult> Data(int departmentId)
    {
        if (!await _userAccessService.CanAccessDepartmentAsync(User, departmentId))
        return Forbid();

        var topology = await _topologyService.GetByDepartmentAsync(departmentId);
        if (topology == null) return NotFound();

        return Json(topology, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutRequest request)
    {
        if (request.DepartmentId <= 0) return BadRequest();
        if (!await _userAccessService.CanEditDepartmentAsync(User, request.DepartmentId))
            return Forbid();

        await _topologyService.SaveLayoutAsync(
            request.DepartmentId,
            request.Positions ?? new(),
            request.Edges     ?? new());

        return Ok(new { saved = true });
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.AdminOrEditor)]
    public async Task<IActionResult> ResetLayout(int departmentId)
    {
        if (!await _userAccessService.CanEditDepartmentAsync(User, departmentId))
            return RedirectToAction("AccessDenied", "Auth");

        await _topologyService.ResetLayoutAsync(departmentId);
        TempData["Success"] = "Topology layout reset to automatic.";
        return RedirectToAction(nameof(Index), new { departmentId });
    }
}

public class SaveLayoutRequest
{
    public int DepartmentId { get; set; }
    public Dictionary<string, NodePosition>? Positions { get; set; }
    public List<CustomEdge>? Edges { get; set; }
}