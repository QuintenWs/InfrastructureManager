using System.Text.Json;
using InfrastructureManager.Application.Interfaces.Services;
using InfrastructureManager.Domain.Entities;
using InfrastructureManager.Infrastructure.Data;
using InfrastructureManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureManager.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _context             = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager         = userManager;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        int entityId,
        string entityLabel,
        object? oldValues = null,
        object? newValues = null)
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;

        string? userId      = null;
        string displayName  = "System";

        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(httpUser);
            if (appUser != null)
            {
                userId      = appUser.Id;
                displayName = $"{appUser.FirstName} {appUser.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = appUser.Email ?? appUser.UserName ?? "Unknown";
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var log = new AuditLog
        {
            UserId          = userId,
            UserDisplayName = displayName,
            Action          = action,
            EntityType      = entityType,
            EntityId        = entityId,
            EntityLabel     = entityLabel,
            OldValues       = oldValues != null ? JsonSerializer.Serialize(oldValues, options) : null,
            NewValues       = newValues != null ? JsonSerializer.Serialize(newValues, options) : null,
            CreatedAt       = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
