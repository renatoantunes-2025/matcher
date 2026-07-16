using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

[Authorize(Roles = nameof(BrokerRole.Admin))]
public class AdminController(MatchRDbContext db, IAuthService authService) : ApiControllerBase
{
    [HttpGet("access-requests")]
    public async Task<IActionResult> GetAccessRequests()
    {
        var requests = await db.AccessRequests
            .Where(r => r.Status == AccessRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
        return Ok(requests);
    }

    [HttpPost("access-requests/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await db.AccessRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (await db.Brokers.AnyAsync(b => b.Email == request.Email))
        {
            return Conflict(new { message = "Já existe um corretor com este e-mail." });
        }

        var tempPassword = Guid.NewGuid().ToString("N")[..10];
        db.Brokers.Add(new Broker
        {
            Name = request.Name,
            Email = request.Email,
            Creci = request.Creci,
            Phone = request.Phone,
            PasswordHash = authService.HashPassword(tempPassword),
            Role = BrokerRole.Broker,
            Status = BrokerStatus.Active
        });

        request.Status = AccessRequestStatus.Approved;
        await db.SaveChangesAsync();

        return Ok(new { message = "Corretor aprovado.", temporaryPassword = tempPassword });
    }

    [HttpPost("access-requests/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var request = await db.AccessRequests.FindAsync(id);
        if (request is null) return NotFound();

        request.Status = AccessRequestStatus.Rejected;
        await db.SaveChangesAsync();
        return Ok(new { message = "Solicitação rejeitada." });
    }

    [HttpGet("brokers")]
    public async Task<IActionResult> GetBrokers()
    {
        var brokers = await db.Brokers.OrderBy(b => b.Name).ToListAsync();
        var dtos = brokers.Select(b => new
        {
            b.Id,
            b.Name,
            b.Email,
            b.Creci,
            Role = b.Role.ToString(),
            Status = b.Status.ToString()
        });
        return Ok(dtos);
    }

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        var totalActive = await db.Properties.CountAsync(p => p.Active);
        var agencyCount = await db.Agencies.CountAsync();
        return Ok(new { totalActive, agencyCount });
    }
}
