using MarketList.Domain.Entities;
using MarketList.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketList.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista usuários (id, login, criadoEm)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _context.Set<Usuario>()
            .AsNoTracking()
            .Select(u => new { id = u.Id, login = u.Login, criadoEm = u.CreatedAt })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    /// <summary>
    /// Exclui usuário por id
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(currentUserId) && Guid.TryParse(currentUserId, out var guidCurrent))
        {
            if (guidCurrent == id)
            {
                return BadRequest(new { error = "Não é permitido excluir o próprio usuário." });
            }
        }

        var totalUsers = await _context.Set<Usuario>().CountAsync();
        if (totalUsers <= 1)
        {
            return BadRequest(new { error = "Não é permitido excluir o último usuário do sistema." });
        }

        var user = await _context.Set<Usuario>().FindAsync(id);
        if (user == null) return NotFound();

        _context.Set<Usuario>().Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
