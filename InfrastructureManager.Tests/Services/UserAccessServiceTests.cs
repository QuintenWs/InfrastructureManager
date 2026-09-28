using System.Security.Claims;
using InfrastructureManager.Domain.Entities;
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
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static Mock<UserManager<ApplicationUser>> UserManagerReturning(string userId)
    {
        var mock = CreateUserManagerMock();
        mock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        return mock;
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
        // Fail-closed: een gebruiker zonder groep/uitzondering ziet NIETS, niet "alles".
        var context = InMemoryDbContextFactory.Create();
        var sut = new UserAccessService(context, UserManagerReturning("viewer-1").Object);

        var result = await sut.GetAccessibleDepartmentIdsAsync(CreatePrincipal("viewer-1", AppRoles.Viewer));

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccessibleDepartmentIdsAsync_ViewerWithLocationGrant_ExpandsToAllDepartmentsInThatLocation()
    {
        var context = InMemoryDbContextFactory.Create();

        var antwerpen     = await TestData.AddLocationAsync(context, "Antwerpen");
        var hasselt       = await TestData.AddLocationAsync(context, "Hasselt");
        var deptA         = await TestData.AddDepartmentAsync(context, "IT", antwerpen);
        var deptB         = await TestData.AddDepartmentAsync(context, "Security", antwerpen);
        var deptElsewhere = await TestData.AddDepartmentAsync(context, "IT", hasselt);
        await TestData.AddUserAsync(context, "viewer-1");

        var group = new AccessGroup { Name = "Antwerpen Team" };
        context.AccessGroups.Add(group);
        await context.SaveChangesAsync();

        context.AccessGroupGrants.Add(new AccessGroupGrant { AccessGroupId = group.Id, LocationId = antwerpen.Id });
        context.UserAccessGroups.Add(new UserAccessGroup { UserId = "viewer-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sut = new UserAccessService(context, UserManagerReturning("viewer-1").Object);

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

        var department = await TestData.AddDepartmentAsync(context);
        await TestData.AddUserAsync(context, "viewer-1");

        var group = new AccessGroup { Name = "Group" };
        context.AccessGroups.Add(group);
        await context.SaveChangesAsync();

        context.AccessGroupGrants.Add(new AccessGroupGrant { AccessGroupId = group.Id, DepartmentId = department.Id });
        context.UserAccessGroups.Add(new UserAccessGroup { UserId = "viewer-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sut = new UserAccessService(context, UserManagerReturning("viewer-1").Object);

        var canEdit = await sut.CanEditDepartmentAsync(CreatePrincipal("viewer-1", AppRoles.Viewer), department.Id);

        Assert.False(canEdit);
    }

    [Fact]
    public async Task CanEditDepartmentAsync_EditorOutsideScope_ReturnsFalse()
    {
        var context = InMemoryDbContextFactory.Create();

        var location         = await TestData.AddLocationAsync(context);
        var accessibleDept   = await TestData.AddDepartmentAsync(context, "Accessible", location);
        var inaccessibleDept = await TestData.AddDepartmentAsync(context, "Inaccessible", location);
        await TestData.AddUserAsync(context, "editor-1");

        var group = new AccessGroup { Name = "Group" };
        context.AccessGroups.Add(group);
        await context.SaveChangesAsync();

        context.AccessGroupGrants.Add(new AccessGroupGrant { AccessGroupId = group.Id, DepartmentId = accessibleDept.Id });
        context.UserAccessGroups.Add(new UserAccessGroup { UserId = "editor-1", AccessGroupId = group.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sut = new UserAccessService(context, UserManagerReturning("editor-1").Object);
        var principal = CreatePrincipal("editor-1", AppRoles.Editor);

        Assert.True(await sut.CanEditDepartmentAsync(principal, accessibleDept.Id));
        Assert.False(await sut.CanEditDepartmentAsync(principal, inaccessibleDept.Id));
    }

    [Fact]
    public async Task GetAccessibleDepartmentCountsAsync_BatchesCorrectlyForMultipleUsers()
    {
        // Regressietest voor de batch-methode uit stap 4.2: zelfde resultaat als
        // de per-gebruiker-methode, voor meerdere gebruikers met verschillende scopes.
        var context = InMemoryDbContextFactory.Create();

        var location = await TestData.AddLocationAsync(context);
        var deptA    = await TestData.AddDepartmentAsync(context, "A", location);
        var deptB    = await TestData.AddDepartmentAsync(context, "B", location);
        await TestData.AddUserAsync(context, "user-1");
        await TestData.AddUserAsync(context, "user-2");

        var groupA = new AccessGroup { Name = "Group A" };
        context.AccessGroups.Add(groupA);
        await context.SaveChangesAsync();

        context.AccessGroupGrants.Add(new AccessGroupGrant { AccessGroupId = groupA.Id, DepartmentId = deptA.Id });
        context.UserAccessGroups.Add(new UserAccessGroup { UserId = "user-1", AccessGroupId = groupA.Id });
        context.UserAccessGrants.Add(new UserAccessGrant { UserId = "user-2", DepartmentId = deptA.Id });
        context.UserAccessGrants.Add(new UserAccessGrant { UserId = "user-2", DepartmentId = deptB.Id });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var sut = new UserAccessService(context, CreateUserManagerMock().Object);

        // "user-3" bestaat niet en heeft geen rijen: moet gewoon 0 geven.
        var counts = await sut.GetAccessibleDepartmentCountsAsync(new[] { "user-1", "user-2", "user-3" });

        Assert.Equal(1, counts["user-1"]);
        Assert.Equal(2, counts["user-2"]);
        Assert.Equal(0, counts.GetValueOrDefault("user-3", 0));
    }
}