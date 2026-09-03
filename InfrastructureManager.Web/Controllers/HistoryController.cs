using InfrastructureManager.Application.DTOs.History;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Web.ViewModels.History;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

// Full audit trail with user attribution across the whole system — this is
// an administrative capability, same tier as Users/Import/DeviceTypes.
[Authorize(Roles = AppRoles.Admin)]
public class HistoryController : Controller
{
    private readonly IHistoryService _historyService;
    private const int PageSize = 20;

    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(HistoryFilterViewModel filter)
    {
        var result = await _historyService.SearchAsync(new HistoryFilter
        {
            UserId     = filter.UserId,
            EntityType = filter.EntityType,
            EntityId   = filter.EntityId,
            Search     = filter.Search,
            FromDate   = filter.FromDate,
            ToDate     = filter.ToDate,
            Page       = filter.Page,
            PageSize   = PageSize
        });

        var users = await _historyService.GetUsersAsync();
        var types = await _historyService.GetEntityTypesAsync();

        var vm = new HistoryIndexViewModel
        {
            Filter = filter,
            Result = result,
            Users = users.Select(u => new SelectListItem { Value = u.UserId, Text = u.DisplayName }).ToList(),
            EntityTypes = types.Select(t => new SelectListItem { Value = t, Text = TranslateType(t) }).ToList()
        };

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = result.Page,
            TotalPages  = result.TotalPages,
            TotalCount  = result.TotalCount,
            RouteValues = new Dictionary<string, string>
            {
                ["UserId"]     = filter.UserId ?? "",
                ["EntityType"] = filter.EntityType ?? "",
                ["EntityId"]   = filter.EntityId?.ToString() ?? "",
                ["Search"]     = filter.Search ?? "",
                ["FromDate"]   = filter.FromDate?.ToString("yyyy-MM-dd") ?? "",
                ["ToDate"]     = filter.ToDate?.ToString("yyyy-MM-dd") ?? ""
            }
        };

        return View(vm);
    }

    public static string TranslateType(string type) => type switch
    {
        "Device"               => "Toestel",
        "Department"           => "Departement",
        "Location"             => "Locatie",
        "Network"              => "Netwerk",
        "Contact"              => "Contact",
        "SiteVisit"            => "Bezoek",
        "ActionItem"           => "Actiepunt",
        "InventoryCheck"       => "Controle",
        "DeviceTypeDefinition" => "Apparaattype",
        "Import"               => "Import",
        _                      => type
    };
}
