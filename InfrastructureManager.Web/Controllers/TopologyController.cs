using InfrastructureManager.Application.DTOs.Topology;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Infrastructure.Data;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class TopologyController : Controller
{
    private readonly ITopologyService _topologyService;
    private readonly AppDbContext     _context;

    public TopologyController(ITopologyService topologyService, AppDbContext context)
    {
        _topologyService = topologyService;
        _context         = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? departmentId)
    {
        var departments = await _context.Departments
            .Include(d => d.Location)
            .OrderBy(d => d.Location.Name).ThenBy(d => d.Name)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text  = $"{d.Name} — {d.Location.Name}"
            })
            .ToListAsync();

        ViewBag.Departments  = departments;
        ViewBag.DepartmentId = departmentId;

        if (!departmentId.HasValue) return View(null as object);

        var topology = await _topologyService.GetByDepartmentAsync(departmentId.Value);
        return View(topology);
    }

    /// <summary>Returns topology as JSON for the JS renderer.</summary>
    [HttpGet]
    public async Task<IActionResult> Data(int departmentId)
    {
        var topology = await _topologyService.GetByDepartmentAsync(departmentId);
        if (topology == null) return NotFound();

        return Json(topology, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>Save drag-and-drop layout. Called via AJAX after each move.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> SaveLayout([FromBody] SaveLayoutRequest request)
    {
        if (request.DepartmentId <= 0) return BadRequest();

        await _topologyService.SaveLayoutAsync(
            request.DepartmentId,
            request.Positions ?? new(),
            request.Edges     ?? new());

        return Ok(new { saved = true });
    }

    /// <summary>Reset layout to automatic. Deletes saved positions.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ResetLayout(int departmentId)
    {
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
