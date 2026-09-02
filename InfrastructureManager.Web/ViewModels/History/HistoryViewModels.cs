using InfrastructureManager.Application.DTOs.History;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.ViewModels.History;

public class HistoryFilterViewModel
{
    public string?   UserId     { get; set; }
    public string?   EntityType { get; set; }
    public int?      EntityId   { get; set; }
    public string?   Search     { get; set; }
    public DateTime? FromDate   { get; set; }
    public DateTime? ToDate     { get; set; }
    public int        Page       { get; set; } = 1;
}

public class HistoryIndexViewModel
{
    public HistoryFilterViewModel Filter { get; set; } = new();
    public HistoryPageResult      Result { get; set; } = new();

    public List<SelectListItem> Users       { get; set; } = new();
    public List<SelectListItem> EntityTypes { get; set; } = new();
}
