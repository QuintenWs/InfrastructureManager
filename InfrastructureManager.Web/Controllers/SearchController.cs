using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Web.ViewModels.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly AppDbContext       _context;
    private readonly IUserAccessService _userAccessService;

    public SearchController(AppDbContext context, IUserAccessService userAccessService)
    {
        _context           = context;
        _userAccessService = userAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View(new GlobalSearchViewModel { Query = string.Empty });

        q = q.Trim();
        var results = new List<SearchResultViewModel>();

        // Dit was voordien de grootste blootstelling: de zoekbalk staat op
        // elke pagina, en doorzocht altijd álle departementen, ongeacht
        // restrictie. Elke deelquery hieronder wordt nu expliciet gescoped.
        var allowedDepartmentIds = await _userAccessService.GetAccessibleDepartmentIdsAsync(User);
        var allowedLocationIds   = await _userAccessService.GetAccessibleLocationIdsAsync(User);

        // ── Devices ───────────────────────────────────────────────────────────
        var deviceQuery = _context.Devices
            .Include(d => d.Department).ThenInclude(dept => dept.Location)
            .Include(d => d.Network)
            .Include(d => d.FieldValues).ThenInclude(v => v.Field)
            .Where(d =>
                d.Name.Contains(q) ||
                d.Department.Name.Contains(q) ||
                d.Department.Location.Name.Contains(q) ||
                d.FieldValues.Any(v => v.Value.Contains(q)))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            deviceQuery = deviceQuery.Where(d => allowedDepartmentIds.Contains(d.DepartmentId));

        var devices = await deviceQuery.OrderBy(d => d.Name).Take(20).ToListAsync();

        results.AddRange(devices.Select(d =>
        {
            var matchingField = d.FieldValues
                .FirstOrDefault(v => v.Value.Contains(q, StringComparison.OrdinalIgnoreCase));

            var detail = matchingField != null
                ? $"{matchingField.Field?.Label}: {matchingField.Value}"
                : d.DeviceType.ToString();

            return new SearchResultViewModel
            {
                Category   = "Device",
                Icon       = "bi-pc-display",
                Id         = d.Id,
                Title      = d.Name,
                Subtitle = $"{d.Department.Name} — {d.Department.Location.Name}",
                Detail     = detail,
                Controller = "Devices",
                Action     = "Details"
            };
        }));

        // ── Networks ──────────────────────────────────────────────────────────
        var networkQuery = _context.Networks
            .Include(n => n.Department)
                .ThenInclude(d => d.Location)
            .Where(n =>
                n.Name.Contains(q)           ||
                n.NetworkAddress.Contains(q) ||
                n.Gateway.Contains(q)        ||
                (n.IspName != null && n.IspName.Contains(q)))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            networkQuery = networkQuery.Where(n => allowedDepartmentIds.Contains(n.DepartmentId));

        var networks = await networkQuery.OrderBy(n => n.Name).Take(10).ToListAsync();

        results.AddRange(networks.Select(n => new SearchResultViewModel
        {
            Category   = "Network",
            Icon       = "bi-diagram-3",
            Id         = n.Id,
            Title      = n.Name,
            Subtitle   = $"{n.Department.Name} — {n.Department.Location.Name}",
            Detail     = $"{n.NetworkAddress}/{n.Cidr}",
            Controller = "Networks",
            Action     = "Details"
        }));

        // ── Locations ─────────────────────────────────────────────────────────
        var locationQuery = _context.Locations
            .Where(l =>
                l.Name.Contains(q) ||
                l.City.Contains(q) ||
                l.Country.Contains(q))
            .AsQueryable();

        if (allowedLocationIds != null)
            locationQuery = locationQuery.Where(l => allowedLocationIds.Contains(l.Id));

        var locations = await locationQuery.OrderBy(l => l.Name).Take(10).ToListAsync();

        results.AddRange(locations.Select(l => new SearchResultViewModel
        {
            Category   = "Location",
            Icon       = "bi-geo-alt",
            Id         = l.Id,
            Title      = l.Name,
            Subtitle   = $"{l.City}, {l.Country}",
            Detail     = string.Empty,
            Controller = "Locations",
            Action     = "Details"
        }));

        // ── Departments ───────────────────────────────────────────────────────
        var departmentQuery = _context.Departments
            .Include(d => d.Location)
            .Where(d =>
                d.Name.Contains(q)    ||
                d.Address.Contains(q) ||
                (d.Description != null && d.Description.Contains(q)))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            departmentQuery = departmentQuery.Where(d => allowedDepartmentIds.Contains(d.Id));

        var departments = await departmentQuery.OrderBy(d => d.Name).Take(10).ToListAsync();

        results.AddRange(departments.Select(d => new SearchResultViewModel
        {
            Category   = "Department",
            Icon       = "bi-building",
            Id         = d.Id,
            Title      = d.Name,
            Subtitle   = $"{d.Location.Name} — {d.Address}",
            Detail     = string.Empty,
            Controller = "Departments",
            Action     = "Details"
        }));

        // ── Contacts ──────────────────────────────────────────────────────────
        var contactQuery = _context.Contacts
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Where(c =>
                c.FirstName.Contains(q) ||
                c.LastName.Contains(q)  ||
                c.Email.Contains(q)     ||
                (c.Role != null && c.Role.Contains(q)) ||
                c.Department.Name.Contains(q))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            contactQuery = contactQuery.Where(c => allowedDepartmentIds.Contains(c.DepartmentId));

        var contacts = await contactQuery.OrderBy(c => c.LastName).Take(10).ToListAsync();

        results.AddRange(contacts.Select(c => new SearchResultViewModel
        {
            Category   = "Contact",
            Icon       = "bi-person-vcard",
            Id         = c.Id,
            Title      = $"{c.FirstName} {c.LastName}".Trim(),
            Subtitle   = $"{c.Department.Name} — {c.Department.Location.Name}",
            Detail     = c.Role ?? c.Email,
            Controller = "Contacts",
            Action     = "Details"
        }));

        // ── Actiepunten (Bezoeken) ────────────────────────────────────────────
        var actionItemQuery = _context.ActionItems
            .Include(a => a.Department).ThenInclude(d => d.Location)
            .Where(a => a.Description.Contains(q))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            actionItemQuery = actionItemQuery.Where(a => allowedDepartmentIds.Contains(a.DepartmentId));

        var actionItems = await actionItemQuery.OrderByDescending(a => a.CreatedAt).Take(10).ToListAsync();

        results.AddRange(actionItems.Select(a => new SearchResultViewModel
        {
            Category   = "Actiepunt",
            Icon       = "bi-clipboard-check",
            Id         = a.DepartmentId,
            Title      = a.Description,
            Subtitle   = $"{a.Department.Name} — {a.Department.Location.Name}",
            Detail     = $"{a.Status} · {a.Priority}",
            Controller = "Departments",
            Action     = "Details"
        }));

        // ── Bezoeken — algemene opmerkingen ───────────────────────────────────
        var visitQuery = _context.SiteVisits
            .Include(v => v.Department).ThenInclude(d => d.Location)
            .Where(v => v.Summary != null && v.Summary.Contains(q))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            visitQuery = visitQuery.Where(v => allowedDepartmentIds.Contains(v.DepartmentId));

        var visits = await visitQuery.OrderByDescending(v => v.VisitDate).Take(10).ToListAsync();

        results.AddRange(visits.Select(v => new SearchResultViewModel
        {
            Category   = "Bezoek",
            Icon       = "bi-calendar-check",
            Id         = v.Id,
            Title      = $"Bezoek {v.VisitDate:dd/MM/yyyy}",
            Subtitle   = $"{v.Department.Name} — {v.Department.Location.Name}",
            Detail     = v.Summary ?? string.Empty,
            Controller = "Visits",
            Action     = "Details"
        }));

        // ── Controles — algemene opmerkingen ──────────────────────────────────
        var checkQuery = _context.InventoryChecks
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Where(c => c.Notes != null && c.Notes.Contains(q))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            checkQuery = checkQuery.Where(c => allowedDepartmentIds.Contains(c.DepartmentId));

        var checks = await checkQuery.OrderByDescending(c => c.CheckDate).Take(10).ToListAsync();

        results.AddRange(checks.Select(c => new SearchResultViewModel
        {
            Category   = "Controle",
            Icon       = "bi-clipboard-data",
            Id         = c.Id,
            Title      = $"Controle {c.CheckDate:dd/MM/yyyy}",
            Subtitle   = $"{c.Department.Name} — {c.Department.Location.Name}",
            Detail     = c.Notes ?? string.Empty,
            Controller = "InventoryChecks",
            Action     = "Details"
        }));

        // ── Controles — opmerking per toestel ─────────────────────────────────
        var checkItemQuery = _context.InventoryCheckItems
            .Include(i => i.InventoryCheck).ThenInclude(c => c.Department).ThenInclude(d => d.Location)
            .Where(i => i.Remark != null && i.Remark.Contains(q))
            .AsQueryable();

        if (allowedDepartmentIds != null)
            checkItemQuery = checkItemQuery.Where(i => allowedDepartmentIds.Contains(i.InventoryCheck.DepartmentId));

        var checkItems = await checkItemQuery.OrderByDescending(i => i.InventoryCheck.CheckDate).Take(10).ToListAsync();

        results.AddRange(checkItems.Select(i => new SearchResultViewModel
        {
            Category   = "Controle",
            Icon       = "bi-clipboard-data",
            Id         = i.InventoryCheckId,
            Title      = i.DeviceName,
            Subtitle   = $"{i.InventoryCheck.Department.Name} — {i.InventoryCheck.Department.Location.Name}",
            Detail     = i.Remark ?? string.Empty,
            Controller = "InventoryChecks",
            Action     = "Details"
        }));

        var ordered = results
            .OrderBy(r => r.Category switch
            {
                "Device"     => 0,
                "Network"    => 1,
                "Location"   => 2,
                "Department" => 3,
                "Contact"    => 4,
                "Bezoek"     => 5,
                "Actiepunt"  => 6,
                "Controle"   => 7,
                _            => 8
            })
            .ThenBy(r => r.Title)
            .ToList();

        return View(new GlobalSearchViewModel { Query = q, Results = ordered });
    }
}