using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace tradex_backend.hubs
{

    [Authorize]
    public class PortfolioHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "User ID not found in token.");
                Context.Abort(); 
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            await Clients.Caller.SendAsync("Joined", $"user-{userId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new HubException("User ID not found in token.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
    }
}