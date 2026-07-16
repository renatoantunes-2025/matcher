using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class FavoritesController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PropertyDto>>> GetAll()
    {
        var favorites = await db.Favorites
            .Include(f => f.Property).ThenInclude(p => p!.Agency)
            .Where(f => f.BrokerId == BrokerId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var dtos = favorites.Select(f => new PropertyDto(
            f.Property!.Id, f.Property.Title, f.Property.Neighborhood, f.Property.City,
            f.Property.Price, f.Property.AreaM2, f.Property.Bedrooms, f.Property.Suites,
            f.Property.ParkingSpots, f.Property.Type.ToString(), f.Property.Purpose.ToString(),
            f.Property.ImageUrl, f.Property.SourceUrl, f.Property.Features,
            f.Property.Agency?.Name ?? string.Empty));

        return Ok(dtos);
    }

    [HttpPost("{propertyId:int}")]
    public async Task<IActionResult> Add(int propertyId)
    {
        var exists = await db.Favorites.AnyAsync(f => f.BrokerId == BrokerId && f.PropertyId == propertyId);
        if (exists) return Ok(new { message = "Já favoritado." });

        var property = await db.Properties.FindAsync(propertyId);
        if (property is null) return NotFound();

        db.Favorites.Add(new Favorite { BrokerId = BrokerId, PropertyId = propertyId });
        await db.SaveChangesAsync();
        return Ok(new { message = "Imóvel adicionado aos favoritos." });
    }

    [HttpDelete("{propertyId:int}")]
    public async Task<IActionResult> Remove(int propertyId)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.BrokerId == BrokerId && f.PropertyId == propertyId);
        if (favorite is null) return NotFound();

        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync();
        return Ok(new { message = "Imóvel removido dos favoritos." });
    }
}
