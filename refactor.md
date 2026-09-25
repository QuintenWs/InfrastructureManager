# InfrastructureManager — Code Review & Refactor-advies

**Datum:** september 2026
**Scope:** alle bestanden zoals meegegeven (Domain, Application, Infrastructure, Web — Controllers/ViewModels/Views/JS/CSS)
**Methode:** volledige manuele read-through, geen compiler/build beschikbaar in mijn omgeving — controleer wijzigingen dus altijd met een build + "Find Usages" voor je iets verwijdert.

Eerst het goede nieuws: dit is geen amateuristische codebase. De toegangscontrole (Admin/Editor/Viewer + AccessGroups + individuele grants, overal fail-closed toegepast), de CSRF-fix, de audit-trail, en de zorgvuldige commentaren rond cascade-delete-gedrag getuigen van iemand die goed nadenkt over randgevallen. Onderstaande lijst is dus vooral "dit kan beter" en "dit is een echte bug", niet "dit is amateuristisch".

---

## 🟡 3. Security


### 3.3 `DevToolsController` — laat dit niet meenemen naar productie

Correct als Admin-only gemarkeerd, en de code bevat zelfs zelf al de opmerking dat dit voor productie verwijderd/omheind moet worden. Aangezien je expliciet naar productie-klaarheid vraagt: **doe dat nu ook echt** — gate hem achter `IWebHostEnvironment.IsDevelopment()` (of verwijder de controller helemaal uit de productie-build via een conditional compile / aparte branch), in plaats van enkel op de rol te vertrouwen. Eén gecompromitteerd Admin-account is anders genoeg om test-/rommeldata in productie te injecteren of (erger) om het patroon te zien hoe testdata + audit-logging precies werkt.

### 3.4 Cookie-beveiliging expliciet maken

`ConfigureApplicationCookie` zet `LoginPath`/`AccessDeniedPath`/`ExpireTimeSpan`/`SlidingExpiration`, maar niet expliciet `Cookie.SecurePolicy`. Voor productie (achter HTTPS, met `UseHsts()` al actief):

```csharp
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite     = SameSiteMode.Lax; // meestal al de default, expliciet is duidelijker
```

3.3 — DevToolsController afschermen voor productie
DevToolsController.cs — enkel de class-declaratie
csharp
// voor
[Authorize(Roles = AppRoles.Admin)]
public class DevToolsController : Controller
{
csharp
// na
// Admin-only ÉN enkel actief in Development — voorkomt dat deze tooling
// (test-locatie/departementen/toestellen aanmaken en weer verwijderen) ooit
// vanuit een productieomgeving bereikbaar is, zelfs voor een Admin-account.
// Filter i.p.v. attribuut, want [Authorize]-achtige attributen kunnen geen
// IWebHostEnvironment injecteren.
[Authorize(Roles = AppRoles.Admin)]
[TypeFilter(typeof(DevelopmentOnlyFilter))]
public class DevToolsController : Controller
{
Nieuw: Web/Filters/DevelopmentOnlyFilter.cs
csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InfrastructureManager.Web.Filters;

/// <summary>
/// Geeft een 404 (niet een 403/AccessDenied — een 404 lekt geen informatie
/// over het bestaan van deze controller) zodra de applicatie niet in
/// Development draait. Gebruikt door DevToolsController zodat de testdata-
/// tooling nooit bereikbaar is buiten lokale ontwikkeling, ongeacht rol.
/// </summary>
public class DevelopmentOnlyFilter : IActionFilter
{
    private readonly IWebHostEnvironment _environment;

    public DevelopmentOnlyFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!_environment.IsDevelopment())
        {
            context.Result = new NotFoundResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

Voeg bovenaan DevToolsController.cs toe: using InfrastructureManager.Web.Filters;

(_Layout.cshtml's "Test Data"-navigatie-item blijft altijd zichtbaar voor Admins, ongeacht omgeving — als je wil dat die link in productie ook helemaal verdwijnt in plaats van naar een 404 te leiden, kan ik die <a> in de layout conditioneel maken op @if (Environment.IsDevelopment()). Zeg het als je dat ook wil; ik heb het nu bewust simpel gehouden tot enkel de controller zelf, want de layout injecteert nog geen IWebHostEnvironment en dat is een kleine extra wijziging op zich.)

3.4 — Cookie SecurePolicy expliciet zetten
Program.cs — ConfigureApplicationCookie
csharp
// voor
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Auth/Login";
    options.AccessDeniedPath  = "/Auth/AccessDenied";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
csharp
// na
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Auth/Login";
    options.AccessDeniedPath  = "/Auth/AccessDenied";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Zorgt ervoor dat de auth-cookie nooit over gewone HTTP verstuurd wordt
    // — passend bij UseHsts() in productie. SameSite=Lax is de ASP.NET Core-
    // default voor Identity-cookies, hier expliciet gemaakt zodat het niet
    // stilzwijgend van framework-defaults afhangt.
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite     = SameSiteMode.Lax;
});

Let op vóór je dit toepast: CookieSecurePolicy.Always weigert de cookie over plain HTTP, dus als je lokaal via http://localhost:5008 test (zoals launchSettings.json standaard doet) in plaats van de https://localhost:7006-variant, kan je jezelf hiermee buitensluiten in dev. Test lokaal via de HTTPS-URL, of gebruik desnoods CookieSecurePolicy.SameAsRequest in Development en Always enkel in productie:

csharp
options.Cookie.SecurePolicy = app.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

(Dat vereist wel dat je dit blok ná var app = builder.Build(); verplaatst, aangezien app.Environment dan pas beschikbaar is — wil je die variant, zeg het en ik geef je de volledige herschikte Program.cs. Voor nu ga ik uit van de eerste, simpelere versie met een vaste Always, ervan uitgaand dat je sowieso via HTTPS test.)

## 🟢 7. suggest new ip 
deze twee mag je nog doen:
Automatische tests. Dit is, eerlijk gezegd, het punt met de meeste impact dat nog openstaat. Er is nergens een testproject in de solution. Kijk naar hoeveel keer we tijdens dit traject een build-fout of een gedragswijziging pas na het draaien ontdekten (de LocationId-migratie, de enum-model-binder, de fields/fieldsById-typo) — een testproject met een basisset unit tests op de Services-laag (vooral DeviceTypeService, NetworkService, UserAccessService — de plekken met de meeste businesslogica) zou dat soort dingen bij toekomstige wijzigingen automatisch opvangen in plaats van pas bij een handmatige dotnet build.
Security-headers-middleware (X-Content-Type-Options: nosniff, Referrer-Policy, evt. een CSP) — staat nergens ingesteld, een paar regels werk, standaard hardening voor een productie-webapp.

Ik wil dat je testen schrijft die ik eventueel ook in postman kan zetten, of gewoon dat ik alles wel goed kan testen dat er zo weinig mogelijk bugs inzitten

En ik snap ook niet, als ik op suggest ip druk, en ik heb "laptop" geselecteerd als device type, dan komt er de melding "This device type has no IP field, copy it manually if needed." Op wat wordt die melding gebaseerd, want een laptop heeft weldegelijk een ip nodig, dus wanneer verschijnt die melding, en wanneer niet?





## testen
Nieuw testproject opzetten
bash
dotnet new xunit -n InfrastructureManager.Tests -o InfrastructureManager.Tests
cd InfrastructureManager.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
dotnet add package Moq --version 4.20.72
dotnet add reference ../InfrastructureManager.Application/InfrastructureManager.Application.csproj
dotnet add reference ../InfrastructureManager.Domain/InfrastructureManager.Domain.csproj
dotnet add reference ../InfrastructureManager.Infrastructure/InfrastructureManager.Infrastructure.csproj
cd ..
dotnet sln add InfrastructureManager.Tests/InfrastructureManager.Tests.csproj

Verwijder de door dotnet new xunit gegenereerde UnitTest1.cs — die vervang je door onderstaande bestanden.

InfrastructureManager.Tests.csproj (ter controle — zou er ongeveer zo moeten uitzien na de commando's hierboven)
xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
    <PackageReference Include="Moq" Version="4.20.72" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\InfrastructureManager.Application\InfrastructureManager.Application.csproj" />
    <ProjectReference Include="..\InfrastructureManager.Domain\InfrastructureManager.Domain.csproj" />
    <ProjectReference Include="..\InfrastructureManager.Infrastructure\InfrastructureManager.Infrastructure.csproj" />
  </ItemGroup>

</Project>
TestHelpers/InMemoryDbContextFactory.cs
csharp
using InfrastructureManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Tests.TestHelpers;

/// <summary>
/// Elke test krijgt zijn eigen, geïsoleerde in-memory database (unieke naam
/// per aanroep) — geen gedeelde state tussen tests, geen opruimcode nodig,
/// en geen echte SQL Server vereist om te draaien.
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
Services/DeviceTypeServiceTests.cs
csharp
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Domain.Exceptions;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class DeviceTypeServiceTests
{
    private static DeviceTypeService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new DeviceTypeService(context, auditMock.Object);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithExistingDevices_ThrowsAndDoesNotDelete()
    {
        // Regressietest voor rapportpunt 1.3: verwijderen van een Device Type
        // dat nog in gebruik is, moet geblokkeerd worden i.p.v. te crashen of
        // stilzwijgend toestellen te "verwezen".
        var sut = CreateSut(out var context);

        var location   = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var device     = new Device { Department = department, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };

        context.AddRange(location, department, definition, device);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteDefinitionAsync(definition.Id));

        Assert.True(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithNoDevices_DeletesSuccessfully()
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Printer, Name = "Printer" };
        context.Add(definition);
        await context.SaveChangesAsync();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceTypeDefinitions.AnyAsync(d => d.Id == definition.Id));
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithFieldValues_RemovesValuesBeforeDeleting()
    {
        // Regressietest voor de tweede helft van 1.3: de NoAction-FK op
        // DeviceFieldValues -> DeviceTypeField mocht nooit meer een
        // onafgevangen DbUpdateException geven, zelfs met historische waarden.
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.NAS, Name = "NAS" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP", FieldKey = "ip_address", FieldType = "text" };
        context.AddRange(definition, field);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = 999, Value = "10.0.0.1" });
        await context.SaveChangesAsync();

        await sut.DeleteDefinitionAsync(definition.Id);

        Assert.False(await context.DeviceFieldValues.AnyAsync());
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_DuplicateIpOnSameNetwork_ThrowsIpConflictException()
    {
        var sut = CreateSut(out var context);

        var location    = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department  = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var network      = new Network { Department = department, Name = "LAN", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" };
        var definition   = new DeviceTypeDefinition { DeviceType = DeviceType.Desktop, Name = "Desktop" };
        var field        = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var existingDevice = new Device { Department = department, Network = network, Name = "PC-01", DeviceType = DeviceType.Desktop, Status = DeviceStatus.Active };

        context.AddRange(location, department, network, definition, field, existingDevice);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = existingDevice.Id, Value = "10.0.0.50" });
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        var ex = await Assert.ThrowsAsync<IpConflictException>(() =>
            sut.ValidateFieldValuesAsync(network.Id, excludeDeviceId: null, fieldValues));

        Assert.Equal("10.0.0.50", ex.IpAddress);
        Assert.Equal("PC-01", ex.ConflictWith);
    }

    [Fact]
    public async Task ValidateFieldValuesAsync_SameIpOnDifferentNetwork_DoesNotThrow()
    {
        // Twee netwerken mogen legitiem hetzelfde privé-IP gebruiken — het
        // conflict is per Network geschoped, niet globaal (zie rapport 2.4).
        var sut = CreateSut(out var context);

        var location   = new Location { Name = "Loc", City = "City", Country = "Country" };
        var department = new Department { Location = location, Name = "Dept", Address = "Addr" };
        var networkA   = new Network { Department = department, Name = "LAN A", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" };
        var networkB   = new Network { Department = department, Name = "LAN B", NetworkAddress = "10.0.1.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.1.1", PrimaryDns = "8.8.8.8" };
        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Desktop, Name = "Desktop" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var existingDevice = new Device { Department = department, Network = networkA, Name = "PC-01", DeviceType = DeviceType.Desktop, Status = DeviceStatus.Active };

        context.AddRange(location, department, networkA, networkB, definition, field, existingDevice);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = existingDevice.Id, Value = "10.0.0.50" });
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, "10.0.0.50" } };

        await sut.ValidateFieldValuesAsync(networkB.Id, excludeDeviceId: null, fieldValues);
        // Geen exception = geslaagd
    }

    [Theory]
    [InlineData("number", "not-a-number")]
    [InlineData("date", "not-a-date")]
    [InlineData("mac", "not-a-mac-address")]
    [InlineData("ipv4", "999.999.999.999")]
    public async Task SaveFieldValuesAsync_InvalidFormatForType_ThrowsDeviceFieldValidationException(string fieldType, string invalidValue)
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Field", FieldKey = "test_field", FieldType = fieldType };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        var fieldValues = new Dictionary<int, string> { { field.Id, invalidValue } };

        await Assert.ThrowsAsync<DeviceFieldValidationException>(() =>
            sut.SaveFieldValuesAsync(device.Id, fieldValues));
    }

    [Fact]
    public async Task SaveFieldValuesAsync_ValidValues_PersistsCorrectly()
    {
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Serial", FieldKey = "serial", FieldType = "text" };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        await sut.SaveFieldValuesAsync(device.Id, new Dictionary<int, string> { { field.Id, "SN-12345" } });

        var saved = await context.DeviceFieldValues.SingleAsync(v => v.DeviceId == device.Id);
        Assert.Equal("SN-12345", saved.Value);
    }

    [Fact]
    public async Task SaveFieldValuesAsync_UnchangedValue_SkipsFormatValidation()
    {
        // Legacy-data van vóór de 6.5-validatie bestond mag een verder
        // ongerelateerde bewerking niet blokkeren (zie rapport 6.5).
        var sut = CreateSut(out var context);

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Other, Name = "TestType" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "Count", FieldKey = "count", FieldType = "number" };
        var device     = new Device { Name = "DEV-01", DeviceType = DeviceType.Other, Status = DeviceStatus.Active };

        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "not-a-number" });
        await context.SaveChangesAsync();

        // Zelfde (formeel ongeldige) waarde opnieuw meesturen — mag niet crashen.
        await sut.SaveFieldValuesAsync(device.Id, new Dictionary<int, string> { { field.Id, "not-a-number" } });
    }
}
Services/DeviceServiceTests.cs
csharp
using InfrastructureManager.Application.DTOs.Devices;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class DeviceServiceTests
{
    private static DeviceService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new DeviceService(auditMock.Object, context);
    }

    [Fact]
    public async Task UpdateAsync_ChangingDeviceType_ClearsOldFieldValues()
    {
        // Regressietest voor rapportpunt 1.2.
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var device = new Device { Department = department, Name = "DEV-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(department, device);
        await context.SaveChangesAsync();

        var oldTypeField = new DeviceTypeField { Label = "MAC Address", FieldKey = "mac_address", FieldType = "text" };
        context.Add(oldTypeField);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceId = device.Id, DeviceTypeFieldId = oldTypeField.Id, Value = "AA:BB:CC:DD:EE:FF" });
        await context.SaveChangesAsync();

        await sut.UpdateAsync(new UpdateDeviceDto
        {
            Id           = device.Id,
            DepartmentId = department.Id,
            Name         = device.Name,
            DeviceType   = DeviceType.Printer, // wisselt van type
            Status       = DeviceStatus.Active
        });

        Assert.Empty(await context.DeviceFieldValues.Where(v => v.DeviceId == device.Id).ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_SameDeviceType_KeepsFieldValues()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var device = new Device { Department = department, Name = "DEV-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(department, device);
        await context.SaveChangesAsync();

        var field = new DeviceTypeField { Label = "MAC Address", FieldKey = "mac_address", FieldType = "text" };
        context.Add(field);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceId = device.Id, DeviceTypeFieldId = field.Id, Value = "AA:BB:CC:DD:EE:FF" });
        await context.SaveChangesAsync();

        await sut.UpdateAsync(new UpdateDeviceDto
        {
            Id           = device.Id,
            DepartmentId = department.Id,
            Name         = "DEV-01-renamed",
            DeviceType   = DeviceType.Switch, // zelfde type, enkel naam wijzigt
            Status       = DeviceStatus.Active
        });

        Assert.Single(await context.DeviceFieldValues.Where(v => v.DeviceId == device.Id).ToListAsync());
    }
}
Services/NetworkServiceTests.cs
csharp
using InfrastructureManager.Application.DTOs.Networks;
using InfrastructureManager.Application.Filters;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Domain.Enums;
using InfrastructureManager.Domain.Exceptions;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class NetworkServiceTests
{
    private static NetworkService CreateSut(out AppDbContext context)
    {
        context = InMemoryDbContextFactory.Create();
        var auditMock = new Mock<IAuditService>();
        return new NetworkService(auditMock.Object, context);
    }

    [Fact]
    public async Task CreateAsync_OverlappingSubnet_ThrowsSubnetValidationException()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        });

        // 192.168.1.128/25 valt volledig binnen 192.168.1.0/24 hierboven.
        await Assert.ThrowsAsync<SubnetValidationException>(() => sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN B",
            NetworkAddress = "192.168.1.128", SubnetMask = "255.255.255.128", Cidr = 25,
            Gateway = "192.168.1.129", PrimaryDns = "8.8.8.8"
        }));
    }

    [Fact]
    public async Task CreateAsync_NonOverlappingSubnet_Succeeds()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        });
        await sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN B",
            NetworkAddress = "192.168.2.0", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.2.1", PrimaryDns = "8.8.8.8"
        });

        Assert.Equal(2, await context.Networks.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_InvalidNetworkAddressForCidr_ThrowsSubnetValidationException()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

        // 192.168.1.5 is geen geldig netwerkadres voor /24 (host-bits gezet).
        await Assert.ThrowsAsync<SubnetValidationException>(() => sut.CreateAsync(new CreateNetworkDto
        {
            DepartmentId = department.Id, Name = "LAN A",
            NetworkAddress = "192.168.1.5", SubnetMask = "255.255.255.0", Cidr = 24,
            Gateway = "192.168.1.1", PrimaryDns = "8.8.8.8"
        }));
    }

    [Fact]
    public async Task FilterPagedAsync_RespectsAllowedDepartmentIds()
    {
        // Beveiligingsregressie: als AllowedDepartmentIds ooit per ongeluk
        // niet meer toegepast wordt, zou dit onmiddellijk falen.
        var sut = CreateSut(out var context);

        var deptA = new Department { Name = "Dept A", Address = "Addr" };
        var deptB = new Department { Name = "Dept B", Address = "Addr" };
        context.AddRange(deptA, deptB);
        await context.SaveChangesAsync();

        context.AddRange(
            new Network { DepartmentId = deptA.Id, Name = "LAN A", NetworkAddress = "10.0.0.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8" },
            new Network { DepartmentId = deptB.Id, Name = "LAN B", NetworkAddress = "10.0.1.0", SubnetMask = "255.255.255.0", Cidr = 24, Gateway = "10.0.1.1", PrimaryDns = "8.8.8.8" });
        await context.SaveChangesAsync();

        var result = await sut.FilterPagedAsync(new NetworkFilter { AllowedDepartmentIds = new[] { deptA.Id } }, page: 1, pageSize: 20);

        Assert.Single(result.Items);
        Assert.Equal("LAN A", result.Items[0].Name);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_FullSubnet_ReturnsNull()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var network = new Network
        {
            Department = department, Name = "LAN", NetworkAddress = "10.0.0.0",
            SubnetMask = "255.255.255.252", Cidr = 30, // slechts 2 bruikbare host-adressen: .1 en .2
            Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8"
        };
        context.AddRange(department, network);
        await context.SaveChangesAsync();

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var device     = new Device { Department = department, Network = network, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        // .1 is al bezet door de Gateway zelf, .2 wordt hier vastgelegd via het device
        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "10.0.0.2" });
        await context.SaveChangesAsync();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        Assert.Null(suggestion);
    }

    [Fact]
    public async Task SuggestNextFreeIpAsync_SkipsGatewayAndOccupiedAddresses()
    {
        var sut = CreateSut(out var context);

        var department = new Department { Name = "Dept", Address = "Addr" };
        var network = new Network
        {
            Department = department, Name = "LAN", NetworkAddress = "10.0.0.0",
            SubnetMask = "255.255.255.248", Cidr = 29, // 6 bruikbare host-adressen: .1 t.e.m. .6
            Gateway = "10.0.0.1", PrimaryDns = "8.8.8.8"
        };
        context.AddRange(department, network);
        await context.SaveChangesAsync();

        var definition = new DeviceTypeDefinition { DeviceType = DeviceType.Switch, Name = "Switch" };
        var field      = new DeviceTypeField { DeviceTypeDefinition = definition, Label = "IP Address", FieldKey = "ip_address", FieldType = "text" };
        var device     = new Device { Department = department, Network = network, Name = "SW-01", DeviceType = DeviceType.Switch, Status = DeviceStatus.Active };
        context.AddRange(definition, field, device);
        await context.SaveChangesAsync();

        context.Add(new DeviceFieldValue { DeviceTypeFieldId = field.Id, DeviceId = device.Id, Value = "10.0.0.2" });
        await context.SaveChangesAsync();

        var suggestion = await sut.SuggestNextFreeIpAsync(network.Id);

        // .1 = Gateway (bezet), .2 = device hierboven (bezet) → eerste vrije is .3
        Assert.Equal("10.0.0.3", suggestion);
    }
}
Services/UserAccessServiceTests.cs
csharp
using System.Security.Claims;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using InfrastructureManager.Infrastructure.Services;
using InfrastructureManager.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace InfrastructureManager.Tests.Services;

public class UserAccessServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task GetAccessibleDepartmentIdsAsync_Admin_ReturnsNull_MeaningUnrestricted()
    {
        var context = InMemoryDbContextFactory.Create();
        var sut = new UserAccessService(context, CreateUserManagerMock().Object);

        var result = await sut.GetAccessibleDepartmentIdsAsync(CreatePrincipal("admin-1", AppRoles.Admin));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAccessibleDepartmentIdsAsync_ViewerWithNoGrants_ReturnsEmptyList_FailClosed()
    {
        // Belangrijke fail-closed garantie: een gebruiker zonder enige
        // toegewezen groep/uitzondering ziet NIETS, niet "alles".
        var context = InMemoryDbContextFactory.Create();
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("viewer-1");

        var sut = new UserAccessService(context, userManagerMock.Object);

        var result = await sut.GetAccessibleDepartmentIdsAsync(CreatePrincipal("viewer-1", AppRoles.Viewer));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccessibleDepartmentIdsAsync_ViewerWithLocationGrant_ExpandsToAllDepartmentsInThatLocation()
    {
        var context = InMemoryDbContextFactory.Create();

        var location = new Location { Name = "Antwerpen", City = "Antwerpen", Country = "Belgium" };
        var deptA    = new Department { Location = location, Name = "IT", Address = "Addr" };
        var deptB    = new Department { Location = location, Name = "Security", Address = "Addr" };
        var otherLocation = new Location { Name = "Hasselt", City = "Hasselt", Country = "Belgium" };
        var deptElsewhere = new Department { Location = otherLocation, Name = "IT", Address = "Addr" };
        context.AddRange(location, deptA, deptB, otherLocation, deptElsewhere);
        await context.SaveChangesAsync();

        var group = new AccessGroup { Name = "Antwerpen Team" };
        context.Add(group);
        await context.SaveChangesAsync();
        context.Add(new AccessGroupGrant { AccessGroupId = group.Id, LocationId = location.Id });
        context.Add(new UserAccessGroup { UserId = "viewer-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("viewer-1");

        var sut = new UserAccessService(context, userManagerMock.Object);

        var result = await sut.GetAccessibleDepartmentIdsAsync(CreatePrincipal("viewer-1", AppRoles.Viewer));

        Assert.NotNull(result);
        Assert.Contains(deptA.Id, result);
        Assert.Contains(deptB.Id, result);
        Assert.DoesNotContain(deptElsewhere.Id, result); // andere locatie, mag niet meetellen
    }

    [Fact]
    public async Task CanEditDepartmentAsync_Viewer_AlwaysFalse_EvenWithinScope()
    {
        var context = InMemoryDbContextFactory.Create();

        var department = new Department { Name = "Dept", Address = "Addr" };
        context.Add(department);
        await context.SaveChangesAsync();

        var group = new AccessGroup { Name = "Group" };
        context.Add(group);
        await context.SaveChangesAsync();
        context.Add(new AccessGroupGrant { AccessGroupId = group.Id, DepartmentId = department.Id });
        context.Add(new UserAccessGroup { UserId = "viewer-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("viewer-1");

        var sut = new UserAccessService(context, userManagerMock.Object);

        var canEdit = await sut.CanEditDepartmentAsync(CreatePrincipal("viewer-1", AppRoles.Viewer), department.Id);

        Assert.False(canEdit);
    }

    [Fact]
    public async Task CanEditDepartmentAsync_EditorOutsideScope_ReturnsFalse()
    {
        var context = InMemoryDbContextFactory.Create();

        var accessibleDept   = new Department { Name = "Accessible", Address = "Addr" };
        var inaccessibleDept = new Department { Name = "Inaccessible", Address = "Addr" };
        context.AddRange(accessibleDept, inaccessibleDept);
        await context.SaveChangesAsync();

        var group = new AccessGroup { Name = "Group" };
        context.Add(group);
        await context.SaveChangesAsync();
        context.Add(new AccessGroupGrant { AccessGroupId = group.Id, DepartmentId = accessibleDept.Id });
        context.Add(new UserAccessGroup { UserId = "editor-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("editor-1");

        var sut = new UserAccessService(context, userManagerMock.Object);
        var principal = CreatePrincipal("editor-1", AppRoles.Editor);

        Assert.True(await sut.CanEditDepartmentAsync(principal, accessibleDept.Id));
        Assert.False(await sut.CanEditDepartmentAsync(principal, inaccessibleDept.Id));
    }

    [Fact]
    public async Task GetAccessibleDepartmentCountsAsync_BatchesCorrectlyForMultipleUsers()
    {
        // Regressietest voor de batch-methode uit stap 4.2: moet exact
        // hetzelfde resultaat geven als de per-gebruiker-methode.
        var context = InMemoryDbContextFactory.Create();

        var deptA = new Department { Name = "A", Address = "Addr" };
        var deptB = new Department { Name = "B", Address = "Addr" };
        context.AddRange(deptA, deptB);
        await context.SaveChangesAsync();

        var groupA = new AccessGroup { Name = "Group A" };
        context.Add(groupA);
        await context.SaveChangesAsync();
        context.Add(new AccessGroupGrant { AccessGroupId = groupA.Id, DepartmentId = deptA.Id });
        context.Add(new UserAccessGroup { UserId = "user-1", AccessGroupId = groupA.Id });

        context.Add(new UserAccessGrant { UserId = "user-2", DepartmentId = deptA.Id });
        context.Add(new UserAccessGrant { UserId = "user-2", DepartmentId = deptB.Id });
        await context.SaveChangesAsync();

        var sut = new UserAccessService(context, CreateUserManagerMock().Object);

        var counts = await sut.GetAccessibleDepartmentCountsAsync(new[] { "user-1", "user-2", "user-3" });

        Assert.Equal(1, counts["user-1"]);
        Assert.Equal(2, counts["user-2"]);
        Assert.Equal(0, counts.GetValueOrDefault("user-3", 0));
    }
}
Draaien
bash
dotnet test