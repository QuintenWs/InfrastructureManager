using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Tests.TestHelpers;

/// <summary>
/// Testcontext voor SQLite: overschrijft enkel de vier kolommen die in
/// AppDbContext als SQL Server-specifiek "varbinary(max)" gedefinieerd zijn
/// (SQLite kent "(max)" niet in een typenaam en geeft dan een syntaxfout bij
/// EnsureCreated). De productiecode blijft ongewijzigd.
/// </summary>
public class SqliteTestDbContext : AppDbContext
{
    public SqliteTestDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DepartmentPhoto>().Property(x => x.ImageData).HasColumnType("BLOB");
        builder.Entity<InventoryCheckItem>().Property(x => x.PhotoData).HasColumnType("BLOB");
        builder.Entity<DeviceDocument>().Property(x => x.FileData).HasColumnType("BLOB");
        builder.Entity<DepartmentDocument>().Property(x => x.FileData).HasColumnType("BLOB");
    }
}

/// <summary>
/// Gebruikt een in-memory SQLite-database i.p.v. EF Core's InMemory-provider:
/// InMemory ondersteunt ExecuteDeleteAsync/ExecuteUpdateAsync niet, en die
/// gebruiken we bewust in de services (rapportpunten 1.2/1.3). SQLite is
/// relationeel en gedraagt zich dichter bij SQL Server.
///
/// Elke aanroep opent een eigen connectie (":memory:" is per connectie een
/// aparte database) — geen gedeelde state tussen tests. Foreign keys worden
/// expliciet aangezet, zodat het gedrag niet van SQLite-defaults afhangt.
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqliteTestDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}