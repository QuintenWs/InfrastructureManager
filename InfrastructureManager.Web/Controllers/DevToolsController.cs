using InfrastructureManager.Application.DTOs.Contacts;
using InfrastructureManager.Application.DTOs.Departments;
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.DTOs.InventoryChecks;
using InfrastructureManager.Application.DTOs.Locations;
using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.DTOs.Visits;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Web.Controllers;

/// <summary>
/// Generates (and cleans up) a realistic, clearly-marked set of test data so
/// the whole application can be exercised end-to-end without touching real
/// data. Built entirely through the existing services, so it goes through
/// the same validation/audit logic as normal use.
///
/// NOTE: this is a development/testing aid. Consider removing this
/// controller (or gating it behind IWebHostEnvironment.IsDevelopment())
/// before a final production launch — for now it's protected by requiring
/// the Admin role.
/// </summary>
[Authorize(Roles = AppRoles.Admin)]
public class DevToolsController : Controller
{
    private const string TestLocationName = "Testomgeving";

    private readonly AppDbContext          _context;
    private readonly ILocationService      _locationService;
    private readonly IDepartmentService    _departmentService;
    private readonly INetworkService       _networkService;
    private readonly IDeviceService        _deviceService;
    private readonly IDeviceTypeService    _deviceTypeService;
    private readonly IContactService       _contactService;
    private readonly IVisitService         _visitService;
    private readonly IInventoryCheckService _checkService;

    public DevToolsController(
        AppDbContext           context,
        ILocationService       locationService,
        IDepartmentService     departmentService,
        INetworkService        networkService,
        IDeviceService         deviceService,
        IDeviceTypeService     deviceTypeService,
        IContactService        contactService,
        IVisitService          visitService,
        IInventoryCheckService checkService)
    {
        _context           = context;
        _locationService   = locationService;
        _departmentService = departmentService;
        _networkService    = networkService;
        _deviceService     = deviceService;
        _deviceTypeService = deviceTypeService;
        _contactService    = contactService;
        _visitService      = visitService;
        _checkService      = checkService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.TestDataExists = await _context.Locations.AnyAsync(l => l.Name == TestLocationName);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SeedTestData()
    {
        if (await _context.Locations.AnyAsync(l => l.Name == TestLocationName))
        {
            TempData["Error"] = "Testdata bestaat al. Verwijder ze eerst als je opnieuw wil genereren.";
            return RedirectToAction(nameof(Index));
        }

        // ── 1. Locatie ───────────────────────────────────────────────────────
        await _locationService.CreateAsync(new CreateLocationDto
        {
            Name    = TestLocationName,
            City    = "Testland",
            Country = "België",
            Notes   = "Automatisch gegenereerde testdata — veilig te verwijderen via Ontwikkelaarstools."
        });
        var location = await _context.Locations.FirstAsync(l => l.Name == TestLocationName);

        // ── 2. Departementen ─────────────────────────────────────────────────
        await _departmentService.CreateAsync(new CreateDepartmentDto
        {
            LocationId = location.Id, Name = "Serverruimte Test", Address = "Teststraat 1",
            Description = "Testdepartement voor netwerk- en serverapparatuur."
        });
        await _departmentService.CreateAsync(new CreateDepartmentDto
        {
            LocationId = location.Id, Name = "IT Kantoor Test", Address = "Teststraat 2",
            Description = "Testdepartement voor kantoortoestellen."
        });
        await _departmentService.CreateAsync(new CreateDepartmentDto
        {
            LocationId = location.Id, Name = "Bewaking Test", Address = "Teststraat 3",
            Description = "Testdepartement voor crypto- en bewakingsapparatuur — bewust nog niet bezocht."
        });

        var deptServer = await _context.Departments.FirstAsync(d => d.Name == "Serverruimte Test" && d.LocationId == location.Id);
        var deptOffice = await _context.Departments.FirstAsync(d => d.Name == "IT Kantoor Test" && d.LocationId == location.Id);
        var deptGuard  = await _context.Departments.FirstAsync(d => d.Name == "Bewaking Test" && d.LocationId == location.Id);

        // ── 3. Netwerken ─────────────────────────────────────────────────────
        await _networkService.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = deptServer.Id, Name = "LAN Test", NetworkAddress = "192.168.90.0",
            SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "192.168.90.1", PrimaryDns = "8.8.8.8",
            IsDhcpEnabled = true, DhcpRangeStart = "192.168.90.100", DhcpRangeEnd = "192.168.90.200",
            IsInternetAccessible = true, VlanId = 90
        });
        await _networkService.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = deptOffice.Id, Name = "Kantoor VLAN", NetworkAddress = "192.168.91.0",
            SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "192.168.91.1", PrimaryDns = "8.8.8.8",
            IsDhcpEnabled = true, IsInternetAccessible = true, VlanId = 91
        });
        await _networkService.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = deptGuard.Id, Name = "Beveiligd VLAN", NetworkAddress = "192.168.92.0",
            SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "192.168.92.1", PrimaryDns = "8.8.8.8",
            IsDhcpEnabled = false, IsInternetAccessible = false, VlanId = 92
        });

        var netServer = await _context.Networks.FirstAsync(n => n.Name == "LAN Test" && n.DepartmentId == deptServer.Id);
        var netOffice = await _context.Networks.FirstAsync(n => n.Name == "Kantoor VLAN" && n.DepartmentId == deptOffice.Id);
        var netGuard  = await _context.Networks.FirstAsync(n => n.Name == "Beveiligd VLAN" && n.DepartmentId == deptGuard.Id);

        // ── 4. Toestellen — serverruimte ─────────────────────────────────────
        var swId  = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptServer.Id, NetworkId = netServer.Id, Name = "SW-TEST-01",  DeviceType = DeviceType.Switch,     Status = DeviceStatus.Active });
        var rtrId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptServer.Id, NetworkId = netServer.Id, Name = "RTR-TEST-01", DeviceType = DeviceType.RouterRed,  Status = DeviceStatus.Active });
        var upsId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptServer.Id, NetworkId = null,         Name = "UPS-TEST-01", DeviceType = DeviceType.UPS,        Status = DeviceStatus.Maintenance });
        var nasId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptServer.Id, NetworkId = netServer.Id, Name = "NAS-TEST-01", DeviceType = DeviceType.NAS,         Status = DeviceStatus.Active });

        await SetFieldValueAsync(DeviceType.Switch,    swId,  "ip_address", "192.168.90.10");
        await SetFieldValueAsync(DeviceType.RouterRed, rtrId, "ip_address", "192.168.90.1");
        await SetFieldValueAsync(DeviceType.NAS,       nasId, "ip_address", "192.168.90.20");

        // ── 5. Toestellen — kantoor ──────────────────────────────────────────
        var pcId  = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptOffice.Id, NetworkId = netOffice.Id, Name = "PC-TEST-01",  DeviceType = DeviceType.Desktop, Status = DeviceStatus.Active });
        var lapId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptOffice.Id, NetworkId = netOffice.Id, Name = "LAP-TEST-01", DeviceType = DeviceType.Laptop,  Status = DeviceStatus.Active });
        var prtId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptOffice.Id, NetworkId = netOffice.Id, Name = "PRT-TEST-01", DeviceType = DeviceType.Printer, Status = DeviceStatus.Active });
        var telId = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptOffice.Id, NetworkId = null,         Name = "TEL-TEST-01", DeviceType = DeviceType.Phone,   Status = DeviceStatus.Active });

        // ── 6. Toestellen — bewaking / crypto ────────────────────────────────
        var crypto1 = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptGuard.Id, NetworkId = netGuard.Id, Name = "CRYPTO-TEST-01", DeviceType = DeviceType.Crypto, Status = DeviceStatus.Active, Notes = "Testtoestel — sleutel vervalt binnenkort." });
        var crypto2 = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptGuard.Id, NetworkId = netGuard.Id, Name = "CRYPTO-TEST-02", DeviceType = DeviceType.Crypto, Status = DeviceStatus.Active, Notes = "Testtoestel — sleutel is al verlopen." });
        var crypto3 = await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptGuard.Id, NetworkId = netGuard.Id, Name = "CRYPTO-TEST-03", DeviceType = DeviceType.Crypto, Status = DeviceStatus.Active, Notes = "Testtoestel — sleutel nog lang geldig." });
        await _deviceService.CreateAsync(new CreateDeviceDto { DepartmentId = deptGuard.Id, NetworkId = null, Name = "SAFE-TEST-01", DeviceType = DeviceType.Safe, Status = DeviceStatus.Active });

        await SetCryptoFieldsAsync(crypto1, "SafeNet Test-A", "SN-1001", "KEY-ALPHA",   DateTime.UtcNow.AddDays(10));
        await SetCryptoFieldsAsync(crypto2, "SafeNet Test-B", "SN-1002", "KEY-BRAVO",   DateTime.UtcNow.AddDays(-5));
        await SetCryptoFieldsAsync(crypto3, "SafeNet Test-C", "SN-1003", "KEY-CHARLIE", DateTime.UtcNow.AddDays(200));

        // ── 7. Contacten ─────────────────────────────────────────────────────
        await _contactService.CreateAsync(new CreateContactDto { DepartmentId = deptServer.Id, FirstName = "Jan",   LastName = "Peeters",    Email = "jan.peeters@test.local",    Phone = "0470 00 00 01", Role = "IT Manager" });
        await _contactService.CreateAsync(new CreateContactDto { DepartmentId = deptOffice.Id, FirstName = "Sofie", LastName = "Willems",    Email = "sofie.willems@test.local",   Phone = "0470 00 00 02", Role = "Office Manager" });
        await _contactService.CreateAsync(new CreateContactDto { DepartmentId = deptGuard.Id,  FirstName = "Marc",  LastName = "De Clerck",  Email = "marc.declerck@test.local",   Phone = "0470 00 00 03", Role = "Security Officer" });

        // ── 8. Bezoeken & actiepunten — serverruimte (volledige historiek) ───
        var visit1Id = await _visitService.CreateVisitAsync(new CreateSiteVisitDto
        {
            DepartmentId = deptServer.Id,
            Summary = "Eerste controlebezoek, alles nagekeken.",
            NewItems = new()
            {
                new NewItemInput { Description = "Kabelgoot los aan de achterkant van het rek", Priority = "High" },
                new NewItemInput { Description = "Firmware van de switch is verouderd",         Priority = "Normal" }
            }
        });
        await SetVisitDateAsync(visit1Id, DateTime.UtcNow.AddDays(-30));

        var openItemsServer = (await _visitService.GetOpenActionItemsByDepartmentAsync(deptServer.Id)).ToList();
        var kabelgoot = openItemsServer.First(i => i.Description.Contains("Kabelgoot"));

        var visit2Id = await _visitService.CreateVisitAsync(new CreateSiteVisitDto
        {
            DepartmentId = deptServer.Id,
            Summary = "Opvolgbezoek.",
            ResolvedItems = new() { new ResolvedItemInput { ActionItemId = kabelgoot.Id, ResolutionNotes = "Vastgemaakt met nieuwe kabelclips." } },
            NewItems = new() { new NewItemInput { Description = "UPS-batterij moet getest worden", Priority = "Low" } }
        });
        await SetVisitDateAsync(visit2Id, DateTime.UtcNow.AddDays(-5));

        var firmwareItem = (await _visitService.GetOpenActionItemsByDepartmentAsync(deptServer.Id))
            .First(i => i.Description.Contains("Firmware"));
        await _visitService.SetInProgressAsync(firmwareItem.Id);

        // ── 9. Bezoek — kantoor (slechts 1, voor variatie) ───────────────────
        var visit3Id = await _visitService.CreateVisitAsync(new CreateSiteVisitDto
        {
            DepartmentId = deptOffice.Id,
            Summary = "Kantoor gecheckt, kleine opmerking.",
            NewItems = new() { new NewItemInput { Description = "Printer maakt een raar geluid bij opstarten", Priority = "Normal" } }
        });
        await SetVisitDateAsync(visit3Id, DateTime.UtcNow.AddDays(-10));

        // Bewaking Test krijgt bewust geen bezoek — om de lege staat te tonen.

        // ── 10. Digitale controles ───────────────────────────────────────────
        var testPhoto = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        var check1Id = await _checkService.CreateAsync(new CreateInventoryCheckDto
        {
            DepartmentId = deptServer.Id,
            Notes = "Maandelijkse controle serverruimte.",
            Items = new()
            {
                new CreateInventoryCheckItemDto { DeviceId = swId,  IsPresent = true },
                new CreateInventoryCheckItemDto { DeviceId = rtrId, IsPresent = true },
                new CreateInventoryCheckItemDto { DeviceId = upsId, IsPresent = false, Remark = "Niet aangetroffen tijdens controle — na te vragen bij de dienst." },
                new CreateInventoryCheckItemDto { DeviceId = nasId, IsPresent = true, PhotoData = testPhoto, PhotoContentType = "image/png", PhotoFileName = "test-foto.png" }
            }
        });
        await SetCheckDateAsync(check1Id, DateTime.UtcNow.AddDays(-3));

        var check2Id = await _checkService.CreateAsync(new CreateInventoryCheckDto
        {
            DepartmentId = deptOffice.Id,
            Notes = "Controle kantoor.",
            Items = new()
            {
                new CreateInventoryCheckItemDto { DeviceId = pcId,  IsPresent = true },
                new CreateInventoryCheckItemDto { DeviceId = lapId, IsPresent = true },
                new CreateInventoryCheckItemDto { DeviceId = prtId, IsPresent = true, Remark = "Stond uit bij aankomst, terug aangezet." },
                new CreateInventoryCheckItemDto { DeviceId = telId, IsPresent = true }
            }
        });
        await SetCheckDateAsync(check2Id, DateTime.UtcNow.AddDays(-1));

        // Bewaking Test krijgt bewust geen controle — om de lege staat te tonen.

        TempData["Success"] = "Testdata aangemaakt onder locatie 'Testomgeving'.";
        return RedirectToAction("Details", "Locations", new { id = location.Id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTestData()
    {
        var location = await _context.Locations.FirstOrDefaultAsync(l => l.Name == TestLocationName);
        if (location == null)
        {
            TempData["Error"] = "Geen testdata gevonden.";
            return RedirectToAction(nameof(Index));
        }

        // Cascades through Departments -> Devices/Networks/Contacts/Visits/
        // ActionItems/InventoryChecks automatically (FK cascade delete).
        await _locationService.DeleteAsync(location.Id);

        TempData["Success"] = "Testdata verwijderd.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SetFieldValueAsync(DeviceType type, int deviceId, string fieldKey, string value)
    {
        var fields = await _deviceTypeService.GetFieldsAsync(type, deviceId);
        var field  = fields?.Fields.FirstOrDefault(f => f.FieldKey == fieldKey);
        if (field == null) return;

        await _deviceTypeService.SaveFieldValuesAsync(deviceId, new Dictionary<int, string> { { field.Id, value } });
    }

    private async Task SetCryptoFieldsAsync(int deviceId, string model, string serial, string keyId, DateTime keyExpiry)
    {
        var fields = await _deviceTypeService.GetFieldsAsync(DeviceType.Crypto, deviceId);
        if (fields == null) return;

        var values = new Dictionary<int, string>();

        void Map(string key, string value)
        {
            var f = fields.Fields.FirstOrDefault(x => x.FieldKey == key);
            if (f != null) values[f.Id] = value;
        }

        Map("model", model);
        Map("serial_number", serial);
        Map("key_id", keyId);
        Map("key_expiry", keyExpiry.ToString("yyyy-MM-dd"));

        await _deviceTypeService.SaveFieldValuesAsync(deviceId, values);
    }

    private async Task SetVisitDateAsync(int visitId, DateTime date)
    {
        var visit = await _context.SiteVisits.FindAsync(visitId);
        if (visit == null) return;
        visit.VisitDate = date;
        visit.CreatedAt = date;
        await _context.SaveChangesAsync();
    }

    private async Task SetCheckDateAsync(int checkId, DateTime date)
    {
        var check = await _context.InventoryChecks.FindAsync(checkId);
        if (check == null) return;
        check.CheckDate = date;
        check.CreatedAt = date;
        await _context.SaveChangesAsync();
    }
}
