using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("dashboard")]
public class DashboardController(MatchRDbContext db) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var vm = new DashboardViewModel
        {
            FirstName = BrokerName.Split(' ').FirstOrDefault() ?? "",
            Clients = await db.Clients.Where(c => c.BrokerId == BrokerId).OrderBy(c => c.Name).ToListAsync(),
            ActiveClients = await db.Clients.CountAsync(c => c.BrokerId == BrokerId),
            SearchesThisMonth = await db.SearchRequests.CountAsync(s => s.BrokerId == BrokerId && s.CreatedAt >= monthStart),
            FavoritedProperties = await db.Favorites.CountAsync(f => f.BrokerId == BrokerId),
            SharesSent = await db.ShareEvents.CountAsync(e => e.BrokerId == BrokerId && e.Type == ShareEventType.WhatsAppShare),
            RecentClients = await db.Clients.Include(c => c.Searches)
                .Where(c => c.BrokerId == BrokerId)
                .OrderByDescending(c => c.LastActivityAt)
                .Take(4)
                .ToListAsync(),
            RecentActivity = await db.ShareEvents.Include(e => e.Client)
                .Where(e => e.BrokerId == BrokerId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(6)
                .ToListAsync(),
        };

        ViewData["Title"] = "Visão geral";
        return View(vm);
    }

    [HttpPost("busca-rapida")]
    public IActionResult QuickSearch(int? clientId, string? briefing)
    {
        return RedirectToAction("Index", "Search", new { clientId, briefing });
    }
}
