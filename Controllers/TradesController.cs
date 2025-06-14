using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using tradex_backend.Models;
namespace tradex_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TradesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> GetMyTrades()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var trades = await _context.Trades
                .Where(t => t.BuyerId == id || t.SellerId == id)
                .OrderByDescending(t => t.ExecutedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Symbol,
                    t.Quantity,
                    t.Price,
                    t.ExecutedAt,
                    Side = t.BuyerId == id ? "Buy" : "Sell",
                    CounterpartyUserId = t.BuyerId == id ? t.SellerId : t.BuyerId
                })
                .ToListAsync();

            return Ok(trades);
        }
    }
}