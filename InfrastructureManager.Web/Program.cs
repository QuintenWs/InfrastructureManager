using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Data.Seed;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));

    // Elk formulier in deze app rendert al een antiforgery-token (via de
    // <form> tag helper of expliciet @Html.AntiForgeryToken()), maar er was
    // nergens een controller/actie die dat token ook effectief valideerde —
    // CSRF-bescherming stond dus overal in de HTML, maar werd nergens
    // afgedwongen. Deze globale filter valideert het token voortaan
    // automatisch op elke POST/PUT/PATCH/DELETE-actie.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    // topology.js stuurt het token via deze header voor zijn JSON-POST
    // (SaveLayout gebruikt [FromBody], dus er is geen formulierveld om het
    // token uit te lezen) — moet exact overeenkomen met wat de JS al verstuurt.
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// /health geeft een load balancer/uptime-monitor een simpel Healthy/Unhealthy-
// signaal, inclusief een échte databank-connectiecheck (niet enkel "de app
// draait" — die zegt niets over of de app ook effectief kan werken).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

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

// Standaard herwaardeert Identity een ingelogde sessie maar elke 30 minuten
// tegen de database. Voor deactivatie (UsersController.ToggleActive) is dat
// te traag voor een "sluit deze persoon nu meteen buiten"-scenario — 2
// minuten is een redelijke middenweg tussen veiligheid en extra DB-load.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(2);
});

builder.Services.AddScoped<IAuditService,               AuditService>();
builder.Services.AddScoped<IDashboardService,           DashboardService>();
builder.Services.AddScoped<IDepartmentService,          DepartmentService>();
builder.Services.AddScoped<ILocationService,            LocationService>();
builder.Services.AddScoped<IDeviceService,              DeviceService>();
builder.Services.AddScoped<IDeviceTypeService,          DeviceTypeService>();
builder.Services.AddScoped<INetworkService,             NetworkService>();
builder.Services.AddScoped<IContactService,             ContactService>();
builder.Services.AddScoped<IFileService,                FileService>();
builder.Services.AddScoped<IImportService,              ImportService>();
builder.Services.AddScoped<IMaintenanceLogService,      MaintenanceLogService>();
builder.Services.AddScoped<ITopologyService,            TopologyService>();
builder.Services.AddScoped<ITemplateService,            TemplateService>();
builder.Services.AddScoped<IVisitService,               VisitService>();
builder.Services.AddScoped<IInventoryCheckService,      InventoryCheckService>();
builder.Services.AddScoped<IHistoryService,              HistoryService>();
builder.Services.AddScoped<IUserAccessService,          UserAccessService>();
builder.Services.AddScoped<IDeviceDocumentService,      DeviceDocumentService>();
builder.Services.AddScoped<IDepartmentDocumentService,  DepartmentDocumentService>();
builder.Services.AddScoped<IExportService,              ExportService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Standaard hardening-headers voor elke response. De CSP hieronder is
// bewust conservatief i.p.v. maximaal streng: deze app haalt Bootstrap/
// Bootstrap Icons/Cytoscape van jsdelivr en gebruikt op meerdere plekken
// inline <script>-blokken (bv. de topologiedata, filter-toggles) — een
// strikte CSP zonder 'unsafe-inline'/nonces zou die meteen breken. Deze
// versie blokkeert wel al <object>/<embed> en het insluiten van de site in
// een <iframe> op een andere origin, zonder iets te breken.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

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