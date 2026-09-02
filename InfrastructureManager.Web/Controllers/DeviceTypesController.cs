using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.DeviceTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DeviceTypesController : Controller
{
    private readonly IDeviceTypeService _service;
    private readonly AppDbContext       _context;

    public DeviceTypesController(
        IDeviceTypeService service,
        AppDbContext        context)
    {
        _service = service;
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var definitions = await _service.GetAllDefinitionsAsync();

        // Count devices per type
        var deviceCounts = await _context.Devices
            .GroupBy(d => (int)d.DeviceType)
            .Select(g => new { TypeValue = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeValue, x => x.Count);

        // Map DeviceType enum value → definition Id via definitions
        var vm = definitions.Select(d => new DeviceTypeListViewModel
        {
            Id          = d.Id,
            Name        = d.Name,
            FieldCount  = d.Fields.Count(),
            DeviceCount = deviceCounts.TryGetValue(
                              (int)_context.DeviceTypeDefinitions
                                  .Where(x => x.Id == d.Id)
                                  .Select(x => (int)x.DeviceType)
                                  .FirstOrDefault(),
                              out var cnt) ? cnt : 0
        });

        return View(vm);
    }

    // ── Details ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var definition = await _service.GetDefinitionByIdAsync(id);
        if (definition == null) return NotFound();

        var deviceCount = await _context.DeviceTypeDefinitions
            .Where(x => x.Id == id)
            .Select(x => _context.Devices.Count(d => d.DeviceType == x.DeviceType))
            .FirstOrDefaultAsync();

        var vm = new DeviceTypeDetailsViewModel
        {
            Id          = definition.Id,
            Name        = definition.Name,
            DeviceCount = deviceCount,
            Fields      = definition.Fields.Select(f => new DeviceTypeFieldViewModel
            {
                Id            = f.Id,
                Label         = f.Label,
                FieldKey      = f.FieldKey,
                FieldType     = f.FieldType,
                SelectOptions = f.SelectOptions,
                IsRequired    = f.IsRequired,
                AlertOnExpiry = f.AlertOnExpiry,
                SortOrder     = f.SortOrder
            }),
            NewField = new AddFieldViewModel { DefinitionId = id }
        };

        return View(vm);
    }

    // ── Create type ───────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create() => View(new CreateDeviceTypeViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateDeviceTypeViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var id = await _service.CreateDefinitionAsync(vm.Name, vm.Description);
        TempData["Success"] = $"Device type '{vm.Name}' created. Add fields below.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Edit type name ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var definition = await _service.GetDefinitionByIdAsync(id);
        if (definition == null) return NotFound();

        return View(new EditDeviceTypeViewModel
        {
            Id          = id,
            Name        = definition.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditDeviceTypeViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _service.UpdateDefinitionAsync(vm.Id, vm.Name, vm.Description);
        TempData["Success"] = "Device type updated.";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Delete type ───────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteDefinitionAsync(id);
        TempData["Success"] = "Device type deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ── Add field ─────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> AddField(AddFieldViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Field label is required.";
            return RedirectToAction(nameof(Details), new { id = vm.DefinitionId });
        }

        await _service.AddFieldAsync(vm.DefinitionId, new()
        {
            Label         = vm.Label,
            FieldType     = vm.FieldType,
            SelectOptions = vm.SelectOptions,
            IsRequired    = vm.IsRequired,
            AlertOnExpiry = vm.AlertOnExpiry
        });

        TempData["Success"] = $"Field '{vm.Label}' added.";
        return RedirectToAction(nameof(Details), new { id = vm.DefinitionId });
    }

    // ── Edit field ────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> EditField(int fieldId, int definitionId)
    {
        var definition = await _service.GetDefinitionByIdAsync(definitionId);
        if (definition == null) return NotFound();

        var field = definition.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (field == null) return NotFound();

        return View(new EditFieldViewModel
        {
            FieldId       = fieldId,
            DefinitionId  = definitionId,
            Label         = field.Label,
            FieldType     = field.FieldType,
            SelectOptions = field.SelectOptions,
            IsRequired    = field.IsRequired,
            AlertOnExpiry = field.AlertOnExpiry
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditField(EditFieldViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _service.UpdateFieldAsync(vm.FieldId, new()
        {
            Label         = vm.Label,
            FieldType     = vm.FieldType,
            SelectOptions = vm.SelectOptions,
            IsRequired    = vm.IsRequired,
            AlertOnExpiry = vm.AlertOnExpiry
        });

        TempData["Success"] = "Field updated.";
        return RedirectToAction(nameof(Details), new { id = vm.DefinitionId });
    }

    // ── Delete field ──────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> DeleteField(int fieldId, int definitionId)
    {
        await _service.DeleteFieldAsync(fieldId);
        TempData["Success"] = "Field deleted.";
        return RedirectToAction(nameof(Details), new { id = definitionId });
    }
}