using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace tradex_backend.hubs;

[Authorize]
public class OrderBookHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"📡 Connected to OrderBookHub: {Context.ConnectionId}");
    }

    public async Task JoinGroup(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"orderbook-{symbol}");
        Console.WriteLine($"✅ Joined orderbook group: orderbook-{symbol}");
    }

    public async Task LeaveGroup(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"orderbook-{symbol}");
        Console.WriteLine($"❎ Left orderbook group: orderbook-{symbol}");
    }
}