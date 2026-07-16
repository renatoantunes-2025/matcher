using MatchR.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class HistoryController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clientId)
    {
        var query = db.ShareEvents
            .Include(e => e.Client)
            .Where(e => e.BrokerId == BrokerId);

        if (clientId is not null) query = query.Where(e => e.ClientId == clientId);

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync();

        var dtos = events.Select(e => new
        {
            e.Id,
            e.CreatedAt,
            ClientName = e.Client!.Name,
            Type = e.Type.ToString(),
            e.Summary,
            e.ResultCount,
            e.SearchRequestId
        });

        return Ok(dtos);
    }
}
