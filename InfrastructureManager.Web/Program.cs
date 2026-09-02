using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Data.Seed;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Application.Interfaces.Repositories;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Repositories;
using InfrastructureManager.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength         = 8;
    options.Password.RequireUppercase       = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireDigit           = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers      = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<CustomSignInManager>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Auth/Login";
    options.AccessDeniedPath  = "/Auth/AccessDenied";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ILocationRepository,   LocationRepository>();
builder.Services.AddScoped<IDeviceRepository,     DeviceRepository>();
builder.Services.AddScoped<INetworkRepository,    NetworkRepository>();
builder.Services.AddScoped<IContactRepository,    ContactRepository>();

builder.Services.AddScoped<IAuditService,          AuditService>();
builder.Services.AddScoped<IDashboardService,      DashboardService>();
builder.Services.AddScoped<IDepartmentService,     DepartmentService>();
builder.Services.AddScoped<ILocationService,       LocationService>();
builder.Services.AddScoped<IDeviceService,         DeviceService>();
builder.Services.AddScoped<IDeviceTypeService,     DeviceTypeService>();
builder.Services.AddScoped<INetworkService,        NetworkService>();
builder.Services.AddScoped<IContactService,        ContactService>();
builder.Services.AddScoped<IFileService,           FileService>();
builder.Services.AddScoped<IImportService,         ImportService>();
builder.Services.AddScoped<IMaintenanceLogService, MaintenanceLogService>();
builder.Services.AddScoped<ITopologyService,       TopologyService>();
builder.Services.AddScoped<ITemplateService,       TemplateService>();
builder.Services.AddScoped<IVisitService,          VisitService>();
builder.Services.AddScoped<IInventoryCheckService, InventoryCheckService>();
builder.Services.AddScoped<IHistoryService,        HistoryService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}

app.Run();
