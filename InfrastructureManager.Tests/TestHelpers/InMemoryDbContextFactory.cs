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