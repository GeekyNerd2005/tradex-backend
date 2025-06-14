using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using tradex_backend.Models;
using tradex_backend.Dtos;
using System.Text.Json;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;

    public PortfolioController(AppDbContext db)
    {
        _db = db;
        _http = new HttpClient();
    }

    [HttpGet]
    public async Task<ActionResult<PortfolioDto>> GetPortfolio()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token.");

        var holdings = await _db.Holdings
            .Where(h => h.UserId == userId && h.Quantity > 0)
            .ToListAsync();

        var result = new PortfolioDto
        {
            UserId = userId,
            Holdings = new List<HoldingDto>()
        };

        foreach (var h in holdings)
        {
            var price = await GetCurrentPrice(h.Symbol);
            if (price == null) continue;

            var pnl = (price.Value - h.AverageBuyPrice) * h.Quantity;

            result.Holdings.Add(new HoldingDto
            {
                Symbol = h.Symbol,
                Quantity = h.Quantity,
                AverageBuyPrice = h.AverageBuyPrice,
                CurrentPrice = price.Value,
                UnrealizedPnL = Math.Round(pnl, 2)
            });
        }

        return Ok(result);
    }

    private async Task<double?> GetCurrentPrice(string symbol)
    {
        try
        {
            var response = await _http.GetAsync($"http://localhost:5050/price?ticker={symbol}");
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
    // Controllers/PortfolioController.cs
    [HttpGet("value-history/{userId}")]
    public async Task<IActionResult> GetPortfolioValueHistory(string userId)
    {
        var snapshots = await _db.PortfolioSnapshots
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Timestamp)
            .ToListAsync();

        return Ok(snapshots);
    }

    [HttpGet("winloss/{userId}")]
    public async Task<IActionResult> GetWinLossStats(string userId)
    {
        var trades = await _db.Trades
            .Where(t => t.SellerId.ToString() == userId)
            .ToListAsync();

        var totalTrades = trades.Count;
        var winningTrades = trades.Count(t => t.RealizedPnL > 0);
        var losingTrades = trades.Count(t => t.RealizedPnL < 0);
        var breakevenTrades = trades.Count(t => t.RealizedPnL == 0);
        var totalPnL = trades.Sum(t => t.RealizedPnL);

        return Ok(new
        {
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            BreakevenTrades = breakevenTrades,
            NetRealizedPnL = totalPnL
        });
    }  //add winloss chart later


}