using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using tradex_backend.Data;
using tradex_backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using tradex_backend.hubs;
public class OrderMatchingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly HttpClient _httpClient;
    private readonly IHubContext<PortfolioHub> _portfolioHub;
    private readonly IHubContext<OrderBookHub> _orderBookHub;
    public OrderMatchingService(IServiceProvider services, IHubContext<PortfolioHub> portfolioHub, IHubContext<OrderBookHub> orderBookHub)
    {
        _services = services;
        _httpClient = new HttpClient();
        _portfolioHub = portfolioHub;
        _orderBookHub = orderBookHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pendingOrders = await db.Orders
                    .Where(o => o.Status == OrderStatus.Pending && o.Quantity > 0)
                    .ToListAsync();

                var symbols = pendingOrders.Select(o => o.Symbol).Distinct();

                foreach (var symbol in symbols)
                {
                    var price = await GetLatestPrice(symbol);
                    if (price == null) continue;

                    var orders = pendingOrders
                        .Where(o => o.Symbol == symbol)
                        .Where(o => o.Type != OrderType.StopLimit && o.Type != OrderType.StopMarket || IsExecutable(o, price.Value))
                        .ToList();

                    await MatchOrdersAsync(orders, db, price.Value);
                }

                await db.SaveChangesAsync();
            }

            await Task.Delay(1000, stoppingToken); // 1 second loop for faster testing
        }
    }

    private async Task<double?> GetLatestPrice(string symbol)
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://localhost:5050/price?ticker={symbol}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("price").GetDouble();
        }
        catch
        {
            return null;
        }
    }

    private async Task MatchOrdersAsync(List<Order> orders, AppDbContext db, double marketPrice)
    {
        var buys = orders
            .Where(o => o.Side == OrderSide.Buy)
            .OrderBy(o => o.CreatedAt)
            .ToList();

        var sells = orders
            .Where(o => o.Side == OrderSide.Sell)
            .OrderBy(o => o.CreatedAt)
            .ToList();

        foreach (var buy in buys)
        {
            foreach (var sell in sells.ToList())
            {
                if (!IsMatchable(buy, sell)) continue;

                var matchedQty = Math.Min(buy.Quantity, sell.Quantity);
                if (matchedQty <= 0) continue;

                var executionPrice = ResolveExecutionPrice(buy, sell, marketPrice);
                var now = DateTime.UtcNow;

                // Get average buy price of seller to calculate realized PnL
var sellerHolding = await db.Holdings
    .AsNoTracking()
    .FirstOrDefaultAsync(h => h.UserId == sell.UserId && h.Symbol == sell.Symbol);

double averageBuyPrice = (sellerHolding?.AverageBuyPrice ?? 0);
double realizedPnL = (executionPrice - averageBuyPrice) * matchedQty;

db.Trades.Add(new Trade
{
    Symbol = buy.Symbol,
    Quantity = matchedQty,
    Price = executionPrice,
    ExecutedAt = now,
    BuyerId = int.Parse(buy.UserId),
    SellerId = int.Parse(sell.UserId),
    RealizedPnL = realizedPnL
});

                buy.Quantity -= matchedQty;
                sell.Quantity -= matchedQty;

                buy.ExecutedPrice = executionPrice;
                buy.ExecutedAt = now;
                if (buy.Quantity <= 0) buy.Status = OrderStatus.Executed;

                sell.ExecutedPrice = executionPrice;
                sell.ExecutedAt = now;
                if (sell.Quantity <= 0) sell.Status = OrderStatus.Executed;

                await UpdateHoldings(db, buy.UserId, buy.Symbol, matchedQty, executionPrice, true);
                await UpdateHoldings(db, sell.UserId, sell.Symbol, matchedQty, executionPrice, false);

                await _portfolioHub.Clients.All.SendAsync("TradeExecuted", new
                {
                    Symbol = buy.Symbol,
                    Price = executionPrice,
                    Quantity = matchedQty,
                    BuyerId = buy.UserId,
                    SellerId = sell.UserId,
                    Timestamp = now
                });


                if (sell.Quantity <= 0)
                    sells.Remove(sell);

                if (buy.Quantity <= 0)
                    break;
            }

            var bids = db.Orders
                .Where(o => o.Symbol == buy.Symbol && o.Status == OrderStatus.Pending && o.Side == OrderSide.Buy)
                .GroupBy(o => o.Price)
                .Select(g => new
                {
                    Price = g.Key,
                    Quantity = g.Sum(o => o.Quantity)
                })
                .OrderByDescending(o => o.Price)
                .ToList();

            var asks = db.Orders
                .Where(o => o.Symbol == buy.Symbol && o.Status == OrderStatus.Pending && o.Side == OrderSide.Sell)
                .GroupBy(o => o.Price)
                .Select(g => new
                {
                    Price = g.Key,
                    Quantity = g.Sum(o => o.Quantity)
                })
                .OrderBy(o => o.Price)
                .ToList();
            Console.WriteLine($"📤 Broadcasting OrderBookUpdated for {buy.Symbol}. Bids: {bids.Count}, Asks: {asks.Count}");

            await _orderBookHub.Clients
                .Group($"orderbook-{buy.Symbol}")
                .SendAsync("OrderBookUpdated", new
                {
                    Symbol = buy.Symbol,
                    Bids = bids,
                    Asks = asks
                });


        }
    }

    private double ResolveExecutionPrice(Order buy, Order sell, double marketPrice)
    {
        if (buy.Type == OrderType.Market && sell.Type == OrderType.Market)
            return marketPrice;

        if (buy.Type == OrderType.Market)
            return sell.Price ?? marketPrice;

        if (sell.Type == OrderType.Market)
            return buy.Price ?? marketPrice;

        return (buy.Price.GetValueOrDefault(marketPrice) + sell.Price.GetValueOrDefault(marketPrice)) / 2.0;
    }

    private bool IsMatchable(Order buy, Order sell)
    {
        if (buy.Symbol != sell.Symbol) return false;
        if (buy.UserId == sell.UserId) return false;

        if (buy.Type == OrderType.Market || sell.Type == OrderType.Market)
            return true;

        return buy.Price >= sell.Price;
    }

    private bool IsExecutable(Order order, double marketPrice)
    {
        return order.Type switch
        {
            OrderType.StopMarket =>
                (order.Side == OrderSide.Buy && marketPrice >= order.StopPrice) ||
                (order.Side == OrderSide.Sell && marketPrice <= order.StopPrice),

            OrderType.StopLimit =>
                (order.Side == OrderSide.Buy && marketPrice >= order.StopPrice) ||
                (order.Side == OrderSide.Sell && marketPrice <= order.StopPrice),

            _ => true // Limit and Market orders always eligible
        };
    }

    private async Task UpdateHoldings(AppDbContext db, string userId, string symbol, double qty, double price, bool isBuy)
{
    var holding = db.Holdings.FirstOrDefault(h => h.UserId == userId && h.Symbol == symbol);

    if (isBuy)
    {
        if (holding == null)
        {
            holding = new Holding
            {
                UserId = userId,
                Symbol = symbol,
                Quantity = qty,
                AverageBuyPrice = price,
                UpdatedAt = DateTime.UtcNow
            };
            db.Holdings.Add(holding);
        }
        else
        {
            var totalCost = holding.Quantity * holding.AverageBuyPrice + qty * price;
            holding.Quantity += qty;
            holding.AverageBuyPrice = totalCost / holding.Quantity;
            holding.UpdatedAt = DateTime.UtcNow;
        }
    }
    else
    {
        if (holding != null)
        {
            holding.Quantity -= qty;
            if (holding.Quantity <= 0)
            {
                db.Holdings.Remove(holding);
            }
            else
            {
                holding.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
    
    // Save changes BEFORE broadcasting
    await db.SaveChangesAsync();

    // ✅ Broadcast updated holdings
    var updatedHoldings = await db.Holdings
        .Where(h => h.UserId == userId)
        .ToListAsync();

    await _portfolioHub.Clients.Group(userId)
        .SendAsync("PortfolioUpdated", updatedHoldings);
}
}
