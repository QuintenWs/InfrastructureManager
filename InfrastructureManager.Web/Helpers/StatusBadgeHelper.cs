using InfrastructureManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InfrastructureManager.Web.Helpers;

public static class StatusBadgeHelper
{
    public static string GetStatusClass(
        DeviceStatus status)
    {
        return status switch
        {
            DeviceStatus.Active =>
                "status-active",

            DeviceStatus.Offline =>
                "status-offline",

            DeviceStatus.Maintenance =>
                "status-maintenance",

            DeviceStatus.Retired =>
                "status-retired",

            _ =>
                "status-default"
        };
    }
}