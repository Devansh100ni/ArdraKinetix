using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskBoard.Infrastructure.Realtime;

[Authorize]
public class TaskBoardHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier 
            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst("nameid")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            if (Guid.TryParse(userId, out var userGuid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userGuid:D}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userGuid.ToString().ToLowerInvariant()}");
            }
        }

        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            if (Guid.TryParse(tenantId, out var tenantGuid))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantGuid:D}");
            }
        }

        // Add to allowed tenants
        var allowedTenants = Context.User?.FindFirst("AllowedTenants")?.Value;
        if (!string.IsNullOrWhiteSpace(allowedTenants))
        {
            var ids = allowedTenants.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in ids)
            {
                if (Guid.TryParse(id, out var tGuid))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tGuid:D}");
                }
            }
        }

        // Add Admin users to global_admin group
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "global_admin");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinTenant(string tenantId)
    {
        if (Guid.TryParse(tenantId, out var parsedTenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId:D}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId.ToString().ToLowerInvariant()}");
        }
    }

    public async Task LeaveTenant(string tenantId)
    {
        if (Guid.TryParse(tenantId, out var parsedTenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId:D}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{parsedTenantId.ToString().ToLowerInvariant()}");
        }
    }
}
