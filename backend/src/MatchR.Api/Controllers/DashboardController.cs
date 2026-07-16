using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class DashboardController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeClients = await db.Clients.CountAsync(c => c.BrokerId == BrokerId);
        var searchesThisMonth = await db.SearchRequests.CountAsync(s => s.BrokerId == BrokerId && s.CreatedAt >= monthStart);
        var favorited = await db.Favorites.CountAsync(f => f.BrokerId == BrokerId);
        var sharesSent = await db.ShareEvents.CountAsync(e => e.BrokerId == BrokerId && e.Type == ShareEventType.WhatsAppShare);

        return Ok(new DashboardStatsDto(activeClients, searchesThisMonth, favorited, sharesSent));
    }

    [HttpGet("recent-clients")]
    public async Task<ActionResult<List<ClientDto>>> GetRecentClients()
    {
        var clients = await db.Clients
            .Include(c => c.Searches)
            .Where(c => c.BrokerId == BrokerId)
            .OrderByDescending(c => c.LastActivityAt)
            .Take(4)
            .ToListAsync();

        var dtos = clients.Select(c => new ClientDto(
            c.Id, c.Name, c.Phone, c.Email, c.Status.ToString(), c.Preferences, c.LastActivityAt, c.Searches.Count));

        return Ok(dtos);
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<List<ActivityItemDto>>> GetRecentActivity()
    {
        var events = await db.ShareEvents
            .Include(e => e.Client)
            .Where(e => e.BrokerId == BrokerId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(6)
            .ToListAsync();

        var dtos = events.Select(e => new ActivityItemDto(
            e.Type == ShareEventType.WhatsAppShare
                ? $"{e.ResultCount} imóveis enviados para {e.Client!.Name}."
                : $"Nova busca salva para {e.Client!.Name}.",
            e.CreatedAt));

        return Ok(dtos);
    }
}
