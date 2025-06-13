using tradex_backend.Data;
using Microsoft.EntityFrameworkCore;
using tradex_backend.Models;
using System.Text.Json;
namespace tradex_backend.services
{
    public class PortfolioSnapshotService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly HttpClient _http = new();

    public PortfolioSnapshotService(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var users = await db.Users.Select(u => u.Id).ToListAsync();

            foreach (var userId in users)
            {
                
                var holdings = await db.Holdings.Where(h => h.UserId == userId.ToString()).ToListAsync();

                double totalValue = 0;

                foreach (var holding in holdings)
                {
                    var symbol = holding.Symbol;
                    var quantity = holding.Quantity;

                    var price = await GetPrice(symbol);
                    if (price == null) continue;

                    totalValue += quantity * price.Value;
                }

                db.PortfolioSnapshots.Add(new PortfolioSnapshot
                {
                    UserId = userId.ToString(),
                    TotalValue = totalValue,
                    Timestamp = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task<double?> GetPrice(string symbol)
    {
        try
        {
            var res = await _http.GetAsync($"http://localhost:5050/price?ticker={symbol}");
            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("price").GetDouble();
        }
        catch
        {
            return null;
        }
    }
}

}