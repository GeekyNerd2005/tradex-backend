using Microsoft.AspNetCore.Mvc;
using tradex_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace tradex_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserBalance(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        return Ok(new { balance = user.Balance });
    }
}
