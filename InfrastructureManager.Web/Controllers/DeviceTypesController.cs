using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.DeviceTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class DeviceTypesController : Controller
{
    private readonly IDeviceTypeService _service;
    private readonly AppDbContext       _context;
    private const int PageSize = 20;

    public DeviceTypesController(
        IDeviceTypeService service,
        AppDbContext        context)
    {
        _service = service;
        _context = context;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var definitions = (await _service.GetAllDefinitionsAsync()).ToList();
        var totalCount  = definitions.Count;

        // Eén query voor de aantallen van alle types tegelijk, i.p.v. voorheen
        // tot 2 losse database-round-trips per rij op de pagina (tot 40
        // queries voor een pagina van 20 types).
        var counts = await _context.Devices
            .GroupBy(d => d.DeviceType)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var vm = new DeviceTypeIndexViewModel
        {
            Items = definitions
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(d => new DeviceTypeListViewModel
                {
                    Id              = d.Id,
                    Name            = d.Name,
                    FieldCount      = d.Fields.Count(),
                    DeviceCount     = counts.TryGetValue(d.DeviceType, out var c) ? c : 0,
                    DeviceTypeValue = (int)d.DeviceType
                }),
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages  = PageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)PageSize),
                TotalCount  = totalCount,
                RouteValues = new Dictionary<string, string>()
            }
        };

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
            Id              = definition.Id,
            Name            = definition.Name,
            DeviceCount     = deviceCount,
            DeviceTypeValue = (int)definition.DeviceType,
            Fields          = definition.Fields.Select(f => new DeviceTypeFieldViewModel
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

    [HttpPost]
    public async Task<IActionResult> Create(CreateDeviceTypeViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var id = await _service.CreateDefinitionAsync(vm.Name, vm.Description);
            TempData["Success"] = $"Device type '{vm.Name}' created. Add fields below.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DbUpdateException)
        {
            // Zeer lage kans: twee gelijktijdige aanmaak-verzoeken die dezelfde
            // volgende DeviceType-waarde berekenen, gevangen door de unieke index
            // op DeviceTypeDefinitions.DeviceType.
            ModelState.AddModelError(string.Empty, "Could not create the device type due to a conflict — please try again.");
            return View(vm);
        }
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
        try
        {
            await _service.DeleteDefinitionAsync(id);
            TempData["Success"] = "Device type deleted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
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

        try
        {
            await _service.AddFieldAsync(vm.DefinitionId, new()
            {
                Label         = vm.Label,
                FieldType     = vm.FieldType,
                SelectOptions = vm.SelectOptions,
                IsRequired    = vm.IsRequired,
                AlertOnExpiry = vm.AlertOnExpiry
            });

            TempData["Success"] = $"Field '{vm.Label}' added.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

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