using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class ClientsController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetAll([FromQuery] string? search)
    {
        var query = db.Clients.Where(c => c.BrokerId == BrokerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        var clients = await query
            .Include(c => c.Searches)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();

        return Ok(clients.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> GetById(int id)
    {
        var client = await db.Clients.Include(c => c.Searches)
            .FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();
        return Ok(ToDto(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(ClientUpsertRequest request)
    {
        var client = new Client
        {
            BrokerId = BrokerId,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Status = request.Status,
            Preferences = request.Preferences
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, ToDto(client));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientDto>> Update(int id, ClientUpsertRequest request)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        client.Name = request.Name;
        client.Phone = request.Phone;
        client.Email = request.Email;
        client.Status = request.Status;
        client.Preferences = request.Preferences;
        client.LastActivityAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(client));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ClientDto ToDto(Client c) => new(
        c.Id, c.Name, c.Phone, c.Email, c.Status.ToString(), c.Preferences, c.LastActivityAt,
        c.Searches.Count);
}
