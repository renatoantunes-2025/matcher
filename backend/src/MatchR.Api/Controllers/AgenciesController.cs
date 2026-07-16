using MatchR.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class AgenciesController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var agencies = await db.Agencies.OrderBy(a => a.Name).Select(a => new { a.Id, a.Name }).ToListAsync();
        return Ok(agencies);
    }
}
