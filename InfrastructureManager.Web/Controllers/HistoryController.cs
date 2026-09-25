using InfrastructureManager.Application.DTOs.History;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Web.ViewModels.History;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InfrastructureManager.Web.ViewModels.Shared;

namespace InfrastructureManager.Web.Controllers;

// Niet langer hard Admin-only: een gebruiker (of een groep waarin hij zit)
// kan het History-recht individueel toegekend krijgen (zie IUserAccessService.
// CanViewHistoryAsync). Admins hebben dit recht altijd. De resultaten blijven
// hoe dan ook beperkt tot de departementen die de gebruiker al mag zien.
[Authorize]
public class HistoryController : Controller
{
    private readonly IHistoryService    _historyService;
    private readonly IUserAccessService _userAccessService;
    private const int PageSize = 20;

    public HistoryController(IHistoryService historyService, IUserAccessService userAccessService)
    {
        _historyService    = historyService;
        _userAccessService = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(HistoryFilterViewModel filter)
    {
        if (!await _userAccessService.CanViewHistoryAsync(User))
            return RedirectToAction("AccessDenied", "Auth");

        var allowedDepartmentIds = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);

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
        }, allowedDepartmentIds);

        var users = await _historyService.GetUsersAsync(allowedDepartmentIds);
        var types = await _historyService.GetEntityTypesAsync(allowedDepartmentIds);

        var vm = new HistoryIndexViewModel
        {
            Filter = filter,
            Result = result,
            Users = users.Select(u => new SelectListItem { Value = u.UserId, Text = u.DisplayName }).ToList(),
            EntityTypes = types.Select(t => new SelectListItem { Value = t, Text = TranslateType(t) }).ToList()
        };

        vm.Pagination = new PaginationViewModel
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
        "Device"               => "Device",
        "Department"           => "Department",
        "Location"             => "Location",
        "Network"              => "Network",
        "Contact"              => "Contact",
        "SiteVisit"            => "Visit",
        "ActionItem"           => "Action Item",
        "InventoryCheck"       => "Check",
        "DeviceTypeDefinition" => "Device Type",
        "Import"               => "Import",
        _                      => type
    };
}