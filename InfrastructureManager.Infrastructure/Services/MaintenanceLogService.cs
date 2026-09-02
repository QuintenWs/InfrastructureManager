using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureManager.Infrastructure.Services;

public class MaintenanceLogService : IMaintenanceLogService
{
    private readonly AppDbContext                    _context;
    private readonly IHttpContextAccessor            _httpContextAccessor;
    private readonly UserManager<ApplicationUser>    _userManager;
    private readonly IAuditService                   _audit;

    public MaintenanceLogService(
        AppDbContext                 context,
        IHttpContextAccessor         httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IAuditService                audit)
    {
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager         = userManager;
        _audit               = audit;
    }

    public async Task<IEnumerable<MaintenanceLogDto>> GetByDeviceAsync(int deviceId)
    {
        return await _context.MaintenanceLogs
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new MaintenanceLogDto
            {
                Id              = x.Id,
                DeviceId        = x.DeviceId,
                UserDisplayName = x.UserDisplayName,
                Note            = x.Note,
                CreatedAt       = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task AddAsync(int deviceId, string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;

        var (userId, display) = await GetCurrentUserAsync();

        _context.MaintenanceLogs.Add(new MaintenanceLog
        {
            DeviceId        = deviceId,
            UserId          = userId,
            UserDisplayName = display,
            Note            = note.Trim(),
            CreatedAt       = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Logged against the Device itself (not a separate "MaintenanceLog"
        // type), so this note shows up directly in that device's own history.
        var device = await _context.Devices.FindAsync(deviceId);
        await _audit.LogAsync("NOTE", "Device", deviceId, device?.Name ?? $"Toestel #{deviceId}",
            newValues: new { Note = note.Trim() });
    }

    public async Task DeleteAsync(int logId)
    {
        var entry = await _context.MaintenanceLogs.FindAsync(logId);
        if (entry == null) return;

        var device = await _context.Devices.FindAsync(entry.DeviceId);

        _context.MaintenanceLogs.Remove(entry);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("DELETE", "Device", entry.DeviceId, device?.Name ?? $"Toestel #{entry.DeviceId}",
            oldValues: new { Note = entry.Note });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(string? userId, string displayName)> GetCurrentUserAsync()
    {
        var httpUser    = _httpContextAccessor.HttpContext?.User;
        string? userId  = null;
        string display  = "System";

        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(httpUser);
            if (appUser != null)
            {
                userId  = appUser.Id;
                display = $"{appUser.FirstName} {appUser.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(display))
                    display = appUser.Email ?? "Unknown";
            }
        }

        return (userId, display);
    }
}
