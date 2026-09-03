using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Location>              Locations             => Set<Location>();
    public DbSet<Department>            Departments           => Set<Department>();
    public DbSet<DepartmentPhoto>       DepartmentPhotos      => Set<DepartmentPhoto>();
    public DbSet<Network>               Networks              => Set<Network>();
    public DbSet<Device>                Devices               => Set<Device>();
    public DbSet<DeviceTypeDefinition>  DeviceTypeDefinitions => Set<DeviceTypeDefinition>();
    public DbSet<DeviceTypeField>       DeviceTypeFields      => Set<DeviceTypeField>();
    public DbSet<DeviceFieldValue>      DeviceFieldValues     => Set<DeviceFieldValue>();
    public DbSet<MaintenanceLog>        MaintenanceLogs       => Set<MaintenanceLog>();
    public DbSet<Contact>               Contacts              => Set<Contact>();
    public DbSet<AuditLog>              AuditLogs             => Set<AuditLog>();
    public DbSet<UserDashboardSettings> UserDashboardSettings => Set<UserDashboardSettings>();
    public DbSet<TopologyLayout>        TopologyLayouts       => Set<TopologyLayout>();
    public DbSet<SiteVisit>             SiteVisits            => Set<SiteVisit>();
    public DbSet<ActionItem>            ActionItems           => Set<ActionItem>();
    public DbSet<InventoryCheck>        InventoryChecks       => Set<InventoryCheck>();
    public DbSet<InventoryCheckItem>    InventoryCheckItems   => Set<InventoryCheckItem>();
    public DbSet<DeviceDocument>        DeviceDocuments       => Set<DeviceDocument>();
    public DbSet<DepartmentDocument>    DepartmentDocuments   => Set<DepartmentDocument>();
    public DbSet<UserLocationAccess>    UserLocationAccess    => Set<UserLocationAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Location>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(100).IsRequired();
        });

        builder.Entity<DepartmentPhoto>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ImageData).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.HasOne(x => x.Department).WithMany(x => x.Photos)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Department>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.HasOne(x => x.Location).WithMany(x => x.Departments)
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Network>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NetworkAddress).HasMaxLength(50).IsRequired();
            entity.Property(x => x.SubnetMask).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Gateway).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.Department).WithMany(x => x.Networks)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Location).WithMany(x => x.Networks)
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Device>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(x => x.Department).WithMany(x => x.Devices)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Location).WithMany(x => x.Devices)
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Network).WithMany(x => x.Devices)
                .HasForeignKey(x => x.NetworkId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<MaintenanceLog>(entity =>
        {
            entity.Property(x => x.Note).IsRequired();
            entity.Property(x => x.UserDisplayName).HasMaxLength(200);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => x.DeviceId);
            entity.HasOne(x => x.Device).WithMany(x => x.MaintenanceLogs)
                .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DeviceTypeDefinition>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.DeviceType).IsUnique();
        });

        builder.Entity<DeviceTypeField>(entity =>
        {
            entity.Property(x => x.Label).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FieldKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FieldType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AlertOnExpiry).HasDefaultValue(false);
            entity.HasOne(x => x.DeviceTypeDefinition).WithMany(x => x.Fields)
                .HasForeignKey(x => x.DeviceTypeDefinitionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.DeviceTypeDefinitionId, x.FieldKey }).IsUnique();
        });

        builder.Entity<DeviceFieldValue>(entity =>
        {
            entity.HasIndex(x => new { x.DeviceId, x.DeviceTypeFieldId }).IsUnique();
            entity.HasOne(x => x.Device).WithMany(x => x.FieldValues)
                .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Field).WithMany(x => x.Values)
                .HasForeignKey(x => x.DeviceTypeFieldId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Contact>(entity =>
        {
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Role).HasMaxLength(100);
            entity.HasOne(x => x.Department).WithMany(x => x.Contacts)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.Property(x => x.Action).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityLabel).HasMaxLength(300);
            entity.Property(x => x.UserDisplayName).HasMaxLength(200);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<UserDashboardSettings>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<TopologyLayout>(entity =>
        {
            entity.HasOne(x => x.Department).WithOne()
                .HasForeignKey<TopologyLayout>(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.DepartmentId).IsUnique();
        });

        builder.Entity<SiteVisit>(entity =>
        {
            entity.Property(x => x.UserDisplayName).HasMaxLength(200);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => x.DepartmentId);
            entity.HasIndex(x => x.VisitDate);
            entity.HasOne(x => x.Department).WithMany(d => d.Visits)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ActionItem>(entity =>
        {
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.CreatedByDisplayName).HasMaxLength(200);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.Property(x => x.ResolvedByDisplayName).HasMaxLength(200);
            entity.Property(x => x.ResolvedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.DepartmentId, x.Status });

            entity.HasOne(x => x.Department).WithMany(d => d.ActionItems)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);

            // ClientSetNull (not SetNull): SQL Server treats SetNull as a
            // cascading action too, and Department already cascades into
            // ActionItem directly — a second cascading path via SiteVisit
            // would make SQL Server reject the schema ("multiple cascade
            // paths"). ClientSetNull configures NO ACTION at the database
            // level and lets EF Core null out the reference itself when it
            // processes a tracked delete of a SiteVisit.
            entity.HasOne(x => x.CreatedDuringVisit).WithMany(v => v.CreatedItems)
                .HasForeignKey(x => x.CreatedDuringVisitId).OnDelete(DeleteBehavior.ClientSetNull);
            entity.HasOne(x => x.ResolvedDuringVisit).WithMany(v => v.ResolvedItems)
                .HasForeignKey(x => x.ResolvedDuringVisitId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        builder.Entity<InventoryCheck>(entity =>
        {
            entity.Property(x => x.UserDisplayName).HasMaxLength(200);
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => x.DepartmentId);
            entity.HasIndex(x => x.CheckDate);
            entity.HasOne(x => x.Department).WithMany(d => d.InventoryChecks)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryCheckItem>(entity =>
        {
            entity.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DeviceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PhotoContentType).HasMaxLength(100);
            entity.Property(x => x.PhotoFileName).HasMaxLength(260);
            entity.Property(x => x.PhotoData).HasColumnType("varbinary(max)");

            entity.HasOne(x => x.InventoryCheck).WithMany(c => c.Items)
                .HasForeignKey(x => x.InventoryCheckId).OnDelete(DeleteBehavior.Cascade);

            // ClientSetNull for the same reason as ActionItem above: Device
            // already cascades from Department directly, so a second
            // cascading path here (Department -> InventoryCheck ->
            // InventoryCheckItem is the path we want to keep as real
            // Cascade) must not also cascade/setnull at the DB level.
            entity.HasOne(x => x.Device).WithMany()
                .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.ClientSetNull);
        });

        builder.Entity<DeviceDocument>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(x => x.FileData).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.HasOne(x => x.Device).WithMany(x => x.Documents)
                .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DepartmentDocument>(entity =>
        {
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(x => x.FileData).HasColumnType("varbinary(max)").IsRequired();
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.HasOne(x => x.Department).WithMany(x => x.Documents)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserLocationAccess>(entity =>
        {
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.LocationId }).IsUnique();
            entity.HasOne(x => x.Location).WithMany()
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
