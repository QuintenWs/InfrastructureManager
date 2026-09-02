using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Web.ViewModels.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View(new GlobalSearchViewModel { Query = string.Empty });

        q = q.Trim();
        var results = new List<SearchResultViewModel>();

        // ── Devices ───────────────────────────────────────────────────────────
        // IpAddress, Hostname, Vendor, Model, SerialNumber are now stored in
        // DeviceFieldValues — search on name, department, location, and field values
        var devices = await _context.Devices
            .Include(d => d.Department)
            .Include(d => d.Location)
            .Include(d => d.Network)
            .Include(d => d.FieldValues).ThenInclude(v => v.Field)
            .Where(d =>
                d.Name.Contains(q) ||
                d.Department.Name.Contains(q) ||
                d.Location.Name.Contains(q) ||
                d.FieldValues.Any(v => v.Value.Contains(q)))
            .OrderBy(d => d.Name)
            .Take(20)
            .ToListAsync();

        results.AddRange(devices.Select(d =>
        {
            // Find the best matching field value to show as detail
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
                Subtitle   = $"{d.Department.Name} — {d.Location.Name}",
                Detail     = detail,
                Controller = "Devices",
                Action     = "Details"
            };
        }));

        // ── Networks ──────────────────────────────────────────────────────────
        var networks = await _context.Networks
            .Include(n => n.Department)
                .ThenInclude(d => d.Location)
            .Where(n =>
                n.Name.Contains(q)           ||
                n.NetworkAddress.Contains(q) ||
                n.Gateway.Contains(q)        ||
                (n.IspName != null && n.IspName.Contains(q)))
            .OrderBy(n => n.Name)
            .Take(10)
            .ToListAsync();

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
        var locations = await _context.Locations
            .Where(l =>
                l.Name.Contains(q) ||
                l.City.Contains(q) ||
                l.Country.Contains(q))
            .OrderBy(l => l.Name)
            .Take(10)
            .ToListAsync();

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
        var departments = await _context.Departments
            .Include(d => d.Location)
            .Where(d =>
                d.Name.Contains(q)    ||
                d.Address.Contains(q) ||
                (d.Description != null && d.Description.Contains(q)))
            .OrderBy(d => d.Name)
            .Take(10)
            .ToListAsync();

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
        var contacts = await _context.Contacts
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Where(c =>
                c.FirstName.Contains(q) ||
                c.LastName.Contains(q)  ||
                c.Email.Contains(q)     ||
                (c.Role != null && c.Role.Contains(q)) ||
                c.Department.Name.Contains(q))
            .OrderBy(c => c.LastName)
            .Take(10)
            .ToListAsync();

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

        // ── Actiepunten (Bezoeken) — inclusief opgeloste, voor historisch opzoeken ──
        var actionItems = await _context.ActionItems
            .Include(a => a.Department).ThenInclude(d => d.Location)
            .Where(a => a.Description.Contains(q))
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

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
        var visits = await _context.SiteVisits
            .Include(v => v.Department).ThenInclude(d => d.Location)
            .Where(v => v.Summary != null && v.Summary.Contains(q))
            .OrderByDescending(v => v.VisitDate)
            .Take(10)
            .ToListAsync();

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
        var checks = await _context.InventoryChecks
            .Include(c => c.Department).ThenInclude(d => d.Location)
            .Where(c => c.Notes != null && c.Notes.Contains(q))
            .OrderByDescending(c => c.CheckDate)
            .Take(10)
            .ToListAsync();

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
        var checkItems = await _context.InventoryCheckItems
            .Include(i => i.InventoryCheck).ThenInclude(c => c.Department).ThenInclude(d => d.Location)
            .Where(i => i.Remark != null && i.Remark.Contains(q))
            .OrderByDescending(i => i.InventoryCheck.CheckDate)
            .Take(10)
            .ToListAsync();

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

    [HttpGet]
    public IActionResult Ip(string? q) =>
        RedirectToAction(nameof(Index), new { q });
}
