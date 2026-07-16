using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("admin")]
[Authorize(Roles = nameof(BrokerRole.Admin))]
public class AdminController(MatchRDbContext db, IAuthService authService) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Administração";
        return View(new AdminViewModel
        {
            PendingRequests = await db.AccessRequests
                .Where(r => r.Status == AccessRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(),
            TotalActiveProperties = await db.Properties.CountAsync(p => p.Active),
            AgencyCount = await db.Agencies.CountAsync()
        });
    }

    [HttpPost("solicitacoes/{id:int}/aprovar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await db.AccessRequests.FindAsync(id);
        if (request is null) return NotFound();

        if (await db.Brokers.AnyAsync(b => b.Email == request.Email))
        {
            TempData["Toast"] = "Já existe um corretor com este e-mail.";
            return RedirectToAction(nameof(Index));
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

        TempData["Toast"] = $"Corretor aprovado. Senha temporária: {tempPassword} (anote agora, não será mostrada de novo).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("solicitacoes/{id:int}/rejeitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var request = await db.AccessRequests.FindAsync(id);
        if (request is null) return NotFound();

        request.Status = AccessRequestStatus.Rejected;
        await db.SaveChangesAsync();

        TempData["Toast"] = "Solicitação rejeitada.";
        return RedirectToAction(nameof(Index));
    }
}
