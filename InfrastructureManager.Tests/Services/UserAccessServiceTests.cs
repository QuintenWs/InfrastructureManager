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