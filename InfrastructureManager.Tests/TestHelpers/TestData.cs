using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;

namespace InfrastructureManager.Tests.TestHelpers;

/// <summary>
/// Bouwt foreign-key-correcte testdata op. Elke methode slaat meteen op en
/// geeft de entiteit terug, zodat tests niet zelf hoeven na te denken over
/// welke ouder-rijen eerst bestaan.
/// </summary>
public static class TestData
{
    public static async Task<Location> AddLocationAsync(AppDbContext context, string? name = null)
    {
        var location = new Location
        {
            Name    = name ?? $"Loc-{Guid.NewGuid():N}",
            City    = "City",
            Country = "Country"
        };
        context.Locations.Add(location);
        await context.SaveChangesAsync();
        return location;
    }

    public static async Task<Department> AddDepartmentAsync(
        AppDbContext context, string name = "Dept", Location? location = null)
    {
        location ??= await AddLocationAsync(context);

        var department = new Department { LocationId = location.Id, Name = name, Address = "Addr" };
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        return department;
    }

    public static async Task<Network> AddNetworkAsync(
        AppDbContext context, int departmentId, string name,
        string networkAddress, string subnetMask, int cidr, string gateway)
    {
        var network = new Network
        {
            DepartmentId   = departmentId,
            Name           = name,
            NetworkAddress = networkAddress,
            SubnetMask     = subnetMask,
            Cidr           = cidr,
            Gateway        = gateway,
            PrimaryDns     = "8.8.8.8"
        };
        context.Networks.Add(network);
        await context.SaveChangesAsync();
        return network;
    }

    public static async Task<Device> AddDeviceAsync(
        AppDbContext context, int departmentId,
        DeviceType type = DeviceType.Other, int? networkId = null, string name = "DEV-01")
    {
        var device = new Device
        {
            DepartmentId = departmentId,
            NetworkId    = networkId,
            Name         = name,
            DeviceType   = type,
            Status       = DeviceStatus.Active
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();
        return device;
    }

    public static async Task<DeviceTypeDefinition> AddDefinitionAsync(
        AppDbContext context, DeviceType type, string name)
    {
        var definition = new DeviceTypeDefinition { DeviceType = type, Name = name };
        context.DeviceTypeDefinitions.Add(definition);
        await context.SaveChangesAsync();
        return definition;
    }

    public static async Task<DeviceTypeField> AddFieldAsync(
        AppDbContext context, int definitionId, string label, string fieldKey, string fieldType = "text")
    {
        var field = new DeviceTypeField
        {
            DeviceTypeDefinitionId = definitionId,
            Label     = label,
            FieldKey  = fieldKey,
            FieldType = fieldType
        };
        context.DeviceTypeFields.Add(field);
        await context.SaveChangesAsync();
        return field;
    }

    public static async Task AddFieldValueAsync(
        AppDbContext context, int deviceId, int fieldId, string value)
    {
        context.DeviceFieldValues.Add(new DeviceFieldValue
        {
            DeviceId          = deviceId,
            DeviceTypeFieldId = fieldId,
            Value             = value
        });
        await context.SaveChangesAsync();
    }

    /// <summary>UserAccessGroup/UserAccessGrant hebben een echte FK naar AspNetUsers.</summary>
    public static async Task AddUserAsync(AppDbContext context, string userId)
    {
        context.Users.Add(new ApplicationUser
        {
            Id                 = userId,
            UserName           = userId,
            NormalizedUserName = userId.ToUpperInvariant(),
            Email              = $"{userId}@test.local",
            NormalizedEmail    = $"{userId}@test.local".ToUpperInvariant(),
            FirstName          = "Test",
            LastName           = "User"
        });
        await context.SaveChangesAsync();
    }
}