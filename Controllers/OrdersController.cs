using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using tradex_backend.Dtos;
using tradex_backend.Models;
using Microsoft.EntityFrameworkCore;
using tradex_backend.hubs;
using Microsoft.AspNetCore.SignalR;
namespace tradex_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // ✅ THIS is what was missing!
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<PortfolioHub> _portfolioHub;
        public OrdersController(AppDbContext context, IHubContext<PortfolioHub> portfolioHub)
        {
            _context = context;
            _portfolioHub = portfolioHub;
        }

        [HttpPost("place")]
public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null)
        return Unauthorized();

    if (!int.TryParse(userId, out int userIdInt))
        return Unauthorized();

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdInt);
    if (user == null)
        return Unauthorized();

    var totalCost = dto.Quantity * dto.Price;

    if (dto.Side == OrderSide.Buy)
    {
        if (user.Balance < totalCost)
            return BadRequest("Insufficient funds to place this buy order.");

        user.Balance -= totalCost;
    }

    var order = new Order
    {
        UserId = userId,
        Symbol = dto.Symbol.ToUpper(),
        Side = dto.Side,
        Type = dto.Type,
        Quantity = dto.Quantity,
        Price = dto.Price,
        StopPrice = dto.StopPrice,
        Status = OrderStatus.Pending,
        OriginalQuantity = dto.Quantity
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    await BroadcastOrderBook(order.Symbol);
    return CreatedAtAction(nameof(PlaceOrder), new { id = order.Id }, order);
}

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt) // Optional: sort latest first
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("orderbook/{symbol}")]
        public async Task<IActionResult> GetOrderBook(string symbol)
        {
            var upperSymbol = symbol.ToUpper();

            var buyOrders = await _context.Orders
                .Where(o =>
                    o.Symbol == upperSymbol &&
                    o.Side == OrderSide.Buy &&
                    o.Status == OrderStatus.Pending &&
                    o.Quantity > 0 &&
                    o.Price != null) // exclude market orders
                .GroupBy(o => o.Price)
                .Select(g => new
                {
                    Price = g.Key,
                    TotalQuantity = g.Sum(o => o.Quantity)
                })
                .OrderByDescending(g => g.Price)
                .ToListAsync();

            var sellOrders = await _context.Orders
                .Where(o =>
                    o.Symbol == upperSymbol &&
                    o.Side == OrderSide.Sell &&
                    o.Status == OrderStatus.Pending &&
                    o.Quantity > 0 &&
                    o.Price != null) // exclude market orders
                .GroupBy(o => o.Price)
                .Select(g => new
                {
                    Price = g.Key,
                    TotalQuantity = g.Sum(o => o.Quantity)
                })
                .OrderBy(g => g.Price)
                .ToListAsync();

            return Ok(new
            {
                Symbol = upperSymbol,
                Bids = buyOrders,
                Asks = sellOrders
            });
        }
        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.Status != OrderStatus.Pending)
                return BadRequest(new { message = "Only pending orders can be cancelled." });
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(userId));
            if (user == null)
                return Unauthorized();

            // Refund logic for BUY orders only
            if (order.Side == OrderSide.Buy && order.Price.HasValue)
            {
                double refundAmount = order.Quantity * order.Price.Value;
                user.Balance += refundAmount;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            await BroadcastOrderBook(order.Symbol);
            return Ok(new { message = $"Order #{id} cancelled successfully." });
        }   

        private async Task BroadcastOrderBook(string symbol)
        {
            var upperSymbol = symbol.ToUpper();

            var bids = await _context.Orders
                .Where(o => o.Symbol == upperSymbol && o.Status == OrderStatus.Pending && o.Side == OrderSide.Buy && o.Price != null)
                .GroupBy(o => o.Price)
                .Select(g => new {
                    Price = g.Key,
                    Quantity = g.Sum(o => o.Quantity)
                })
                .OrderByDescending(o => o.Price)
                .ToListAsync();

            var asks = await _context.Orders
                .Where(o => o.Symbol == upperSymbol && o.Status == OrderStatus.Pending && o.Side == OrderSide.Sell && o.Price != null)
                .GroupBy(o => o.Price)
                .Select(g => new {
                    Price = g.Key,
                    Quantity = g.Sum(o => o.Quantity)
                })
                .OrderBy(o => o.Price)
                .ToListAsync();

            await _portfolioHub.Clients.Group(upperSymbol).SendAsync("OrderBookUpdated", new
            {
                Symbol = upperSymbol,
                Bids = bids,
                Asks = asks
            });

        }



    }
}