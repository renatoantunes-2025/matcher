using MatchR.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("historico")]
public class HistoryController(MatchRDbContext db) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var events = await db.ShareEvents.Include(e => e.Client)
            .Where(e => e.BrokerId == BrokerId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();

        ViewData["Title"] = "Histórico";
        return View(events);
    }
}
