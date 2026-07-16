using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(MatchRDbContext db, IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var broker = await db.Brokers.FirstOrDefaultAsync(b => b.Email == request.Email);
        if (broker is null || !authService.VerifyPassword(request.Password, broker.PasswordHash))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        if (broker.Status != BrokerStatus.Active)
        {
            return Unauthorized(new { message = "Cadastro ainda não aprovado." });
        }

        var token = authService.GenerateToken(broker);
        return Ok(new LoginResponse(token, broker.Name, broker.Email, broker.Role.ToString()));
    }

    [HttpPost("access-requests")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestAccess(AccessRequestDto request)
    {
        db.AccessRequests.Add(new AccessRequest
        {
            Name = request.Name,
            Creci = request.Creci,
            Email = request.Email,
            Phone = request.Phone
        });
        await db.SaveChangesAsync();
        return Ok(new { message = "Solicitação enviada." });
    }

    [HttpGet("me")]
    public async Task<ActionResult<LoginResponse>> Me()
    {
        var broker = await db.Brokers.FindAsync(BrokerIdFromClaims());
        if (broker is null) return NotFound();
        return Ok(new LoginResponse(string.Empty, broker.Name, broker.Email, broker.Role.ToString()));
    }

    private int BrokerIdFromClaims() =>
        int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);
}
