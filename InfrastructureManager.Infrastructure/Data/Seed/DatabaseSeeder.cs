using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser>? userManager = null,
        RoleManager<IdentityRole>?    roleManager = null)
    {
        // ── Roles ─────────────────────────────────────────────────────────────
        if (roleManager != null)
        {
            foreach (var role in new[] { AppRoles.Admin, AppRoles.Viewer })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ── Locations + Departments ───────────────────────────────────────────
        if (!await context.Locations.AnyAsync())
        {
            var hasselt   = new Location { Name = "Hasselt",   City = "Hasselt",   Country = "Belgium" };
            var antwerpen = new Location { Name = "Antwerpen", City = "Antwerpen", Country = "Belgium" };
            var brussel   = new Location { Name = "Brussel",   City = "Brussel",   Country = "Belgium" };

            context.Locations.AddRange(hasselt, antwerpen, brussel);
            await context.SaveChangesAsync();

            context.Departments.AddRange(
                new Department { LocationId = hasselt.Id,   Name = "IT Operations", Address = "Gouverneur Roppesingel 25, 3500 Hasselt" },
                new Department { LocationId = antwerpen.Id, Name = "IT Operations", Address = "Generaal Wahislaan 34, 2000 Antwerpen" },
                new Department { LocationId = antwerpen.Id, Name = "Security",      Address = "Generaal Wahislaan 40, 2000 Antwerpen" },
                new Department { LocationId = brussel.Id,   Name = "Infrastructure",Address = "Koningsstraat 12, 1000 Brussel" }
            );
            await context.SaveChangesAsync();
        }

        // ── Device Type Definitions ───────────────────────────────────────────
        if (!await context.DeviceTypeDefinitions.AnyAsync())
        {
            var definitions = new List<DeviceTypeDefinition>
            {
                Def(DeviceType.Switch, "Switch",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("port_count",    "Port Count",      "number"),
                    Field("vlan",          "VLAN",            "text")),

                Def(DeviceType.RouterRed, "Router RED",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("serial",        "Serial Number",   "text")),

                Def(DeviceType.RouterBlack, "Router BLACK",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("serial",        "Serial Number",   "text")),

                Def(DeviceType.Crypto, "Crypto",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("crypto_key",    "Crypto Key ID",   "text")),

                Def(DeviceType.Firewall, "Firewall",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("policy_version","Policy Version",  "text")),

                Def(DeviceType.SFP, "SFP",
                    Field("type",          "SFP Type",        "text"),
                    Field("speed",         "Speed",           "text"),
                    Field("connector",     "Connector Type",  "text")),

                Def(DeviceType.MediaConverter, "Media Converter",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("input_type",    "Input Type",      "text"),
                    Field("output_type",   "Output Type",     "text")),

                Def(DeviceType.WPC, "WPC",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("os_version",    "OS Version",      "text"),
                    Field("cpu",           "CPU",             "text"),
                    Field("ram_gb",        "RAM (GB)",        "number")),

                Def(DeviceType.BPC, "BPC",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("os_version",    "OS Version",      "text"),
                    Field("cpu",           "CPU",             "text"),
                    Field("ram_gb",        "RAM (GB)",        "number")),

                Def(DeviceType.WSKit, "WS Kit",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("components",    "Components",      "text")),

                Def(DeviceType.Desktop, "Desktop",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("os_version",    "OS Version",      "text"),
                    Field("cpu",           "CPU",             "text"),
                    Field("ram_gb",        "RAM (GB)",        "number")),

                Def(DeviceType.Laptop, "Laptop",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("os_version",    "OS Version",      "text"),
                    Field("cpu",           "CPU",             "text"),
                    Field("ram_gb",        "RAM (GB)",        "number")),

                Def(DeviceType.ExtraScreen, "Extra Screen",
                    Field("model",         "Model",           "text"),
                    Field("resolution",    "Resolution",      "text"),
                    Field("size_inch",     "Size (inch)",     "number")),

                Def(DeviceType.Printer, "Printer",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("print_type",    "Print Type",      "select", options: "Laser,Inkjet,Thermal")),

                Def(DeviceType.UPS, "UPS",
                    Field("capacity_va",   "Capacity (VA)",   "number"),
                    Field("battery_count", "Battery Count",   "number"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("runtime_min",   "Runtime (min)",   "number")),

                Def(DeviceType.Safe, "Safe",
                    Field("brand",         "Brand",           "text"),
                    Field("combination",   "Combination Type","select", options: "Digital,Key,Mechanical")),

                Def(DeviceType.NAS, "NAS",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("storage_tb",    "Storage (TB)",    "number"),
                    Field("disk_count",    "Disk Count",      "number")),

                Def(DeviceType.Phone, "Phone",
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("extension",     "Extension",       "text")),

                Def(DeviceType.VTC, "VTC",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("codec",         "Codec",           "text")),

                Def(DeviceType.Armadillo, "Armadillo",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("serial",        "Serial Number",   "text")),

                Def(DeviceType.RAP, "RAP",
                    Field("ip_address",    "IP Address",      "text",   required: true),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text"),
                    Field("ssid",          "SSID",            "text")),

                Def(DeviceType.SAR, "SAR",
                    Field("ip_address",    "IP Address",      "text"),
                    Field("mac_address",   "MAC Address",     "text"),
                    Field("image_version", "Image Version",   "text")),

                Def(DeviceType.STPCables, "STP Cables",
                    Field("cable_count",   "Cable Count",     "number"),
                    Field("length_m",      "Total Length (m)","number"),
                    Field("category",      "Category",        "select", options: "Cat5e,Cat6,Cat6a,Cat7")),

                Def(DeviceType.USBRed, "USB RED",
                    Field("capacity_gb",   "Capacity (GB)",   "number"),
                    Field("serial",        "Serial Number",   "text")),

                Def(DeviceType.USBBlack, "USB BLACK",
                    Field("capacity_gb",   "Capacity (GB)",   "number"),
                    Field("serial",        "Serial Number",   "text")),

                Def(DeviceType.Other, "Other",
                    Field("description",   "Description",     "text")),
            };

            context.DeviceTypeDefinitions.AddRange(definitions);
            await context.SaveChangesAsync();
        }

        // Runs on every startup (cheap, idempotent) — adds the new crypto fields
        // to installations that were already seeded before this update.
        await EnsureCryptoFieldsAsync(context);

        // ── Default admin user ────────────────────────────────────────────────
        if (userManager != null && !await userManager.Users.AnyAsync())
        {
            var admin = new ApplicationUser
            {
                UserName       = "admin@infra.local",
                Email          = "admin@infra.local",
                FirstName      = "Admin",
                LastName       = "User",
                IsActive       = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin!123");

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to seed admin user: {errors}");
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DeviceTypeDefinition Def(
        DeviceType type,
        string name,
        params DeviceTypeField[] fields)
    {
        var def = new DeviceTypeDefinition { DeviceType = type, Name = name };
        var order = 1;
        foreach (var f in fields)
        {
            f.SortOrder = order++;
            def.Fields.Add(f);
        }
        return def;
    }

    private static DeviceTypeField Field(
        string key,
        string label,
        string fieldType = "text",
        bool   required  = false,
        string? options  = null,
        bool   alertOnExpiry = false) => new()
    {
        FieldKey      = key,
        Label         = label,
        FieldType     = fieldType,
        IsRequired    = required,
        SelectOptions = options,
        AlertOnExpiry = alertOnExpiry
    };

    /// <summary>
    /// Ensures the Crypto device type has Model, Serial Number, Key ID and
    /// Key Expiry fields — added by key rather than replacing the whole
    /// definition, so it is safe to run against a database that was already
    /// seeded (and already has devices with values) before this update.
    /// </summary>
    private static async Task EnsureCryptoFieldsAsync(AppDbContext context)
    {
        var cryptoDef = await context.DeviceTypeDefinitions
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.DeviceType == DeviceType.Crypto);

        // Fresh database: Def(DeviceType.Crypto, ...) above hasn't run yet in
        // this call, or the type doesn't exist for another reason — nothing to do.
        if (cryptoDef == null) return;

        var existingKeys = cryptoDef.Fields
            .Select(f => f.FieldKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nextOrder = cryptoDef.Fields.Any() ? cryptoDef.Fields.Max(f => f.SortOrder) : 0;
        var toAdd = new List<DeviceTypeField>();

        void AddIfMissing(string key, string label, string type, bool alertOnExpiry = false)
        {
            if (existingKeys.Contains(key)) return;
            nextOrder++;
            toAdd.Add(new DeviceTypeField
            {
                DeviceTypeDefinitionId = cryptoDef.Id,
                FieldKey      = key,
                Label         = label,
                FieldType     = type,
                AlertOnExpiry = alertOnExpiry,
                SortOrder     = nextOrder
            });
        }

        AddIfMissing("model",         "Model",               "text");
        AddIfMissing("serial_number", "Serienummer",         "text");
        AddIfMissing("key_id",        "Sleutel-ID",          "text");
        AddIfMissing("key_expiry",    "Vervaldatum sleutel", "date", alertOnExpiry: true);

        if (toAdd.Count > 0)
        {
            context.DeviceTypeFields.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }
}