using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MarketList.Application.Interfaces;
using MarketList.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MarketList.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MarketList.Infrastructure.Data.AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;

    public AuthController(MarketList.Infrastructure.Data.AppDbContext context, IPasswordService passwordService, IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await _context.Set<Usuario>().FirstOrDefaultAsync(u => u.Login == req.Login);
        if (user == null) return Unauthorized(new { error = "Invalid credentials" });

        if (!_passwordService.VerificarSenha(user.SenhaHash, req.Senha))
            return Unauthorized(new { error = "Invalid credentials" });

        var token = GenerateToken(user);
        return Ok(new { token });
    }

    [Authorize]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarRequest req)
    {
        var exists = await _context.Set<Usuario>().AnyAsync(u => u.Login == req.Login);
        if (exists) return Conflict(new { error = "Login já existente" });

        var user = new Usuario
        {
            Login = req.Login,
            SenhaHash = _passwordService.HashSenha(req.Senha)
        };

        _context.Set<Usuario>().Add(user);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize]
    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

        var user = await _context.Set<Usuario>().FindAsync(guid);
        if (user == null) return Unauthorized();

        if (!_passwordService.VerificarSenha(user.SenhaHash, req.SenhaAtual))
            return Unauthorized(new { error = "Senha atual inválida" });

        user.SenhaHash = _passwordService.HashSenha(req.NovaSenha);
        await _context.SaveChangesAsync();
        return Ok();
    }

    private string GenerateToken(Usuario user)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = jwt.GetValue<string>("Key") ?? string.Empty;
        var issuer = jwt.GetValue<string>("Issuer");
        var audience = jwt.GetValue<string>("Audience");
        var expMinutes = jwt.GetValue<int>("ExpirationMinutes", 60);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public record LoginRequest(string Login, string Senha);
    public record RegistrarRequest(string Login, string Senha);
    public record AlterarSenhaRequest(string SenhaAtual, string NovaSenha);
}
