using ClosedXML.Excel;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class ImportResult
{
    public bool   Success           { get; set; }
    public int    LocationsAdded    { get; set; }
    public int    DepartmentsAdded  { get; set; }
    public int    NetworksAdded     { get; set; }
    public int    DevicesAdded      { get; set; }
    public string? ErrorMessage     { get; set; }
    public string? ErrorSheet       { get; set; }
    public int?   ErrorRow          { get; set; }
}

public interface IImportService
{
    Task<ImportResult> ImportAsync(IFormFile file);
}

public class ImportService : IImportService
{
    private readonly AppDbContext  _context;
    private readonly IAuditService _audit;

    // Vaste kolomposities op het centrale Devices-tabblad
    private const int V_DEPT     = 1;  // Department Name *
    private const int V_NAME     = 2;  // Device Name *
    private const int V_TYPE     = 3;  // Device Type *
    private const int V_STATUS   = 4;  // Status *
    private const int V_NET      = 5;  // Network Name (optioneel)
    private const int V_NOTES    = 6;  // Notes (optioneel)
    private const int V_CF_START = 7;  // Custom Field 1, 2, 3... beginnen hier

    public ImportService(AppDbContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    public async Task<ImportResult> ImportAsync(IFormFile file)
    {
        if (file.Length == 0)
            return Fail(null, null, "The uploaded file is empty.");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        IXLWorkbook wb;
        try { wb = new XLWorkbook(ms); }
        catch { return Fail(null, null, "Could not read the file. Make sure it is a valid .xlsx file."); }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
            var result = new ImportResult();

            // ── Vaste tabbladen (altijd verplicht) ─────────────────────────────
            var newLocations = await ImportLocationsAsync(GetSheet(wb, "Locations"), result);
            await _context.SaveChangesAsync();

            var newDepts = await ImportDepartmentsAsync(GetSheet(wb, "Departments"), newLocations, result);
            await _context.SaveChangesAsync();

            await ImportNetworksAsync(GetSheet(wb, "Networks"), newDepts, result);
            await _context.SaveChangesAsync();

            // ── Devices — één centraal tabblad voor alle apparaattypes ──────────
            var definitions = await _context.DeviceTypeDefinitions
                .Include(d => d.Fields)
                .ToListAsync();

            var devicesSheet = GetSheet(wb, "Devices");
            if (devicesSheet.RowsUsed().Skip(2).Any())
            {
                await ImportDevicesAsync(devicesSheet, definitions, newDepts, result);
                await _context.SaveChangesAsync();
            }

            await tx.CommitAsync();

            await _audit.LogAsync("CREATE", "Import", 0, file.FileName,
                newValues: new {
                    result.LocationsAdded, result.DepartmentsAdded,
                    result.NetworksAdded,  result.DevicesAdded
                });

            result.Success = true;
            return result;
        }
        catch (ImportException ex)
        {
            await tx.RollbackAsync();
            return Fail(ex.Sheet, ex.Row, ex.Message);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Fail(null, null, $"Unexpected error: {ex.Message}");
        }
    }

    // ── Locations ─────────────────────────────────────────────────────────────

    private async Task<Dictionary<string, Location>> ImportLocationsAsync(
        IXLWorksheet sheet, ImportResult result)
    {
        var added = new Dictionary<string, Location>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in DataRows(sheet))
        {
            var name    = Required(sheet.Name, row, 1, "Name");
            var city    = Required(sheet.Name, row, 2, "City");
            var country = Required(sheet.Name, row, 3, "Country");
            var notes   = Cell(row, 4);

            var existing = await _context.Locations.FirstOrDefaultAsync(l => l.Name == name);
            if (existing != null) { added[name] = existing; continue; }

            var entity = new Location
            {
                Name    = name, City = city, Country = country,
                Notes   = string.IsNullOrWhiteSpace(notes) ? null : notes
            };
            _context.Locations.Add(entity);
            await _context.SaveChangesAsync();
            added[name] = entity;
            result.LocationsAdded++;
        }
        return added;
    }

    // ── Departments ───────────────────────────────────────────────────────────

    private async Task<List<Department>> ImportDepartmentsAsync(
        IXLWorksheet sheet,
        Dictionary<string, Location> locationMap,
        ImportResult result)
    {
        var added = new List<Department>();
        foreach (var row in DataRows(sheet))
        {
            var locName = Required(sheet.Name, row, 1, "Location Name");
            var name    = Required(sheet.Name, row, 2, "Name");
            var address = Required(sheet.Name, row, 3, "Address");
            var desc    = Cell(row, 4);
            var notes   = Cell(row, 5);

            if (!locationMap.TryGetValue(locName, out var location))
            {
                location = await _context.Locations.FirstOrDefaultAsync(l => l.Name == locName);
                if (location == null)
                    throw new ImportException(sheet.Name, row.RowNumber(),
                        $"Location '{locName}' not found. Add it in the Locations sheet first.");
            }

            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == name && d.LocationId == location.Id);
            if (existing != null) { added.Add(existing); continue; }

            var entity = new Department
            {
                LocationId  = location.Id, Name = name, Address = address,
                Description = string.IsNullOrWhiteSpace(desc)  ? null : desc,
                Notes       = string.IsNullOrWhiteSpace(notes) ? null : notes
            };
            _context.Departments.Add(entity);
            await _context.SaveChangesAsync();
            added.Add(entity);
            result.DepartmentsAdded++;
        }
        return added;
    }

    // ── Networks ──────────────────────────────────────────────────────────────

    private async Task ImportNetworksAsync(
        IXLWorksheet sheet, List<Department> depts, ImportResult result)
    {
        foreach (var row in DataRows(sheet))
        {
            var deptName = Required(sheet.Name, row, 1,  "Department Name");
            var name     = Required(sheet.Name, row, 2,  "Name");
            var netAddr  = Required(sheet.Name, row, 3,  "Network Address");
            var subnet   = Required(sheet.Name, row, 4,  "Subnet Mask");
            var cidrStr  = Required(sheet.Name, row, 5,  "CIDR");
            var gateway  = Required(sheet.Name, row, 6,  "Gateway");
            var priDns   = Required(sheet.Name, row, 7,  "Primary DNS");
            var secDns   = Cell(row, 8);
            var dhcpStr  = Required(sheet.Name, row, 9,  "Is DHCP Enabled");
            var intStr   = Required(sheet.Name, row, 10, "Is Internet Accessible");
            var vlanStr  = Cell(row, 11);
            var isp      = Cell(row, 12);
            var notes    = Cell(row, 13);

            var dept = await ResolveDeptAsync(sheet.Name, row, deptName, depts);

            if (!int.TryParse(cidrStr, out var cidr) || cidr < 0 || cidr > 32)
                throw new ImportException(sheet.Name, row.RowNumber(), $"CIDR '{cidrStr}' must be 0–32.");
            if (!bool.TryParse(dhcpStr, out var isDhcp))
                throw new ImportException(sheet.Name, row.RowNumber(), $"'Is DHCP Enabled' must be TRUE or FALSE.");
            if (!bool.TryParse(intStr, out var isInternet))
                throw new ImportException(sheet.Name, row.RowNumber(), $"'Is Internet Accessible' must be TRUE or FALSE.");

            var ipUint = ParseIp(netAddr);
            if (ipUint == null)
                throw new ImportException(sheet.Name, row.RowNumber(), $"'{netAddr}' is not a valid IPv4 address.");

            var mask    = CidrMask(cidr);
            var network = ipUint.Value & mask;
            if (network != ipUint.Value)
                throw new ImportException(sheet.Name, row.RowNumber(),
                    $"'{netAddr}' is not valid for /{cidr}. Did you mean '{UintToIp(network)}'?");

            var entity = new Network
            {
                DepartmentId = dept.Id, LocationId = dept.LocationId, Name = name,
                NetworkAddress = netAddr, SubnetMask = subnet, Cidr = cidr,
                Gateway = gateway, PrimaryDns = priDns,
                SecondaryDns         = secDns ?? string.Empty,
                IsDhcpEnabled        = isDhcp,
                IsInternetAccessible = isInternet,
                VlanId  = int.TryParse(vlanStr, out var vlan) ? vlan : null,
                IspName = string.IsNullOrWhiteSpace(isp)   ? null : isp,
                Notes   = string.IsNullOrWhiteSpace(notes) ? null : notes
            };
            _context.Networks.Add(entity);
            await _context.SaveChangesAsync();
            result.NetworksAdded++;
        }
    }

    // ── Devices — één centraal tabblad voor alle apparaattypes ───────────────────

    private async Task ImportDevicesAsync(
        IXLWorksheet sheet,
        List<Domain.Entities.DeviceTypeDefinition> definitions,
        List<Department> depts,
        ImportResult result)
    {
        var defByName = definitions.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

        // Data begint op rij 3 (rij 1 = header, rij 2 = subtitel)
        var dataRows = sheet.RowsUsed().Skip(2).Where(r =>
        {
            var first = r.Cell(1).GetString().Trim();
            return !first.StartsWith("←") && !string.IsNullOrWhiteSpace(first);
        });

        foreach (var row in dataRows)
        {
            var deptName  = Required(sheet.Name, row, V_DEPT,   "Department Name");
            var name      = Required(sheet.Name, row, V_NAME,   "Device Name");
            var typeName  = Required(sheet.Name, row, V_TYPE,   "Device Type");
            var statusStr = Required(sheet.Name, row, V_STATUS, "Status");
            var netName   = Cell(row, V_NET);
            var notes     = Cell(row, V_NOTES);

            var dept = await ResolveDeptAsync(sheet.Name, row, deptName, depts);

            if (!defByName.TryGetValue(typeName, out var definition))
                throw new ImportException(sheet.Name, row.RowNumber(),
                    $"Onbekend apparaattype '{typeName}'. Kies een waarde uit de dropdown in kolom 'Device Type', of check het tabblad 'Veldoverzicht'.");

            if (!Enum.TryParse<DeviceStatus>(statusStr, ignoreCase: true, out var status))
                throw new ImportException(sheet.Name, row.RowNumber(),
                    $"Unknown Status '{statusStr}'. Use Active, Offline, Maintenance or Retired.");

            int? networkId = null;
            if (!string.IsNullOrWhiteSpace(netName))
            {
                var net = await _context.Networks
                    .FirstOrDefaultAsync(n => n.Name == netName && n.DepartmentId == dept.Id);
                if (net == null)
                    throw new ImportException(sheet.Name, row.RowNumber(),
                        $"Network '{netName}' not found in department '{dept.Name}'.");
                networkId = net.Id;
            }

            var device = new Device
            {
                DepartmentId = dept.Id,
                LocationId   = dept.LocationId,
                NetworkId    = networkId,
                Name         = name,
                DeviceType   = definition.DeviceType,
                Status       = status,
                Notes        = string.IsNullOrWhiteSpace(notes) ? null : notes
            };
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            // De N-de Custom Field-kolom komt overeen met het N-de veld (op SortOrder)
            // van het gekozen apparaattype — zie ook tabblad 'Veldoverzicht'.
            var orderedFields = definition.Fields.OrderBy(f => f.SortOrder).ToList();
            for (int i = 0; i < orderedFields.Count; i++)
            {
                var field = orderedFields[i];
                var value = row.Cell(V_CF_START + i).GetString().Trim();

                if (field.IsRequired && string.IsNullOrWhiteSpace(value))
                    throw new ImportException(sheet.Name, row.RowNumber(),
                        $"Veld '{field.Label}' (Custom Field {i + 1}) is verplicht voor {definition.Name} maar is leeg.");

                if (string.IsNullOrWhiteSpace(value)) continue;

                _context.DeviceFieldValues.Add(new DeviceFieldValue
                {
                    DeviceId          = device.Id,
                    DeviceTypeFieldId = field.Id,
                    Value             = value
                });
            }

            await _context.SaveChangesAsync();
            result.DevicesAdded++;
        }
    }

    // ── Department resolver ───────────────────────────────────────────────────

    private async Task<Department> ResolveDeptAsync(
        string sheet, IXLRow row, string cellValue, List<Department> importedDepts)
    {
        string deptName     = cellValue.Trim();
        string? locationHint = null;

        foreach (var sep in new[] { " – ", " — ", " - " })
        {
            var idx = cellValue.IndexOf(sep, StringComparison.Ordinal);
            if (idx > 0)
            {
                deptName     = cellValue[..idx].Trim();
                locationHint = cellValue[(idx + sep.Length)..].Trim();
                break;
            }
        }

        var matches = importedDepts
            .Where(d => string.Equals(d.Name, deptName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1) return matches[0];

        if (matches.Count > 1 && locationHint != null)
        {
            var ids  = matches.Select(d => d.LocationId).Distinct().ToList();
            var locs = await _context.Locations.Where(l => ids.Contains(l.Id)).ToListAsync();
            var hint = locationHint;
            var byLoc = matches.FirstOrDefault(d =>
            {
                var loc = locs.FirstOrDefault(l => l.Id == d.LocationId);
                return loc != null && loc.Name.Contains(hint, StringComparison.OrdinalIgnoreCase);
            });
            if (byLoc != null) return byLoc;
        }

        if (matches.Count > 0) return matches[0];

        var dbMatches = await _context.Departments
            .Include(d => d.Location)
            .Where(d => d.Name == deptName)
            .ToListAsync();

        if (dbMatches.Count == 1) return dbMatches[0];

        if (dbMatches.Count > 1 && locationHint != null)
        {
            var hint  = locationHint;
            var byLoc = dbMatches.FirstOrDefault(d =>
                d.Location.Name.Contains(hint, StringComparison.OrdinalIgnoreCase));
            if (byLoc != null) return byLoc;
        }

        if (dbMatches.Count > 0) return dbMatches[0];

        throw new ImportException(sheet, row.RowNumber(),
            $"Department '{deptName}' not found. " +
            (locationHint != null ? $"(location: '{locationHint}') " : "") +
            "Add it in the Departments sheet first.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IXLWorksheet GetSheet(IXLWorkbook wb, string name)
    {
        if (!wb.TryGetWorksheet(name, out var sheet))
            throw new ImportException(name, null, $"Sheet '{name}' not found. Use the correct template.");
        return sheet;
    }

    private static IEnumerable<IXLRow> DataRows(IXLWorksheet sheet) =>
        sheet.RowsUsed().Skip(1);

    private static string Required(string sheet, IXLRow row, int col, string field)
    {
        var val = row.Cell(col).GetString().Trim();
        if (string.IsNullOrWhiteSpace(val))
            throw new ImportException(sheet, row.RowNumber(),
                $"'{field}' is required but was empty on row {row.RowNumber()}.");
        return val;
    }

    private static string? Cell(IXLRow row, int col)
    {
        var val = row.Cell(col).GetString().Trim();
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    private static uint? ParseIp(string ip)
    {
        if (!System.Net.IPAddress.TryParse(ip, out var addr)) return null;
        var b = addr.GetAddressBytes();
        if (b.Length != 4) return null;
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static uint CidrMask(int cidr) =>
        cidr == 0 ? 0u : cidr == 32 ? 0xFFFFFFFFu : 0xFFFFFFFFu << (32 - cidr);

    private static string UintToIp(uint ip) =>
        $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";

    private static ImportResult Fail(string? sheet, int? row, string msg) => new()
    {
        Success = false, ErrorMessage = msg, ErrorSheet = sheet, ErrorRow = row
    };
}

public class ImportException : Exception
{
    public string Sheet { get; }
    public int?   Row   { get; }
    public ImportException(string sheet, int? row, string message) : base(message)
    { Sheet = sheet; Row = row; }
}