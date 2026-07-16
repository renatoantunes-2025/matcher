using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class PropertiesController(MatchRDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PropertyDto>>> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = db.Properties.Include(p => p.Agency).AsQueryable();
        if (activeOnly) query = query.Where(p => p.Active);

        var properties = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(properties.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDto>> GetById(int id)
    {
        var property = await db.Properties.Include(p => p.Agency).FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();
        return Ok(ToDto(property));
    }

    [HttpPost]
    public async Task<ActionResult<PropertyDto>> Create(PropertyUpsertRequest request)
    {
        var agency = await GetOrCreateAgencyAsync(request.Agency);

        var property = new Property
        {
            AgencyId = agency.Id,
            Title = request.Title,
            Neighborhood = request.Neighborhood,
            City = request.City,
            Price = request.Price,
            AreaM2 = request.AreaM2,
            Bedrooms = request.Bedrooms,
            Suites = request.Suites,
            ParkingSpots = request.ParkingSpots,
            Type = request.Type,
            Purpose = request.Purpose,
            ImageUrl = request.ImageUrl,
            SourceUrl = request.SourceUrl,
            Features = request.Features
        };

        db.Properties.Add(property);
        await db.SaveChangesAsync();
        property.Agency = agency;
        return CreatedAtAction(nameof(GetById), new { id = property.Id }, ToDto(property));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyDto>> Update(int id, PropertyUpsertRequest request)
    {
        var property = await db.Properties.Include(p => p.Agency).FirstOrDefaultAsync(p => p.Id == id);
        if (property is null) return NotFound();

        var agency = await GetOrCreateAgencyAsync(request.Agency);

        property.AgencyId = agency.Id;
        property.Title = request.Title;
        property.Neighborhood = request.Neighborhood;
        property.City = request.City;
        property.Price = request.Price;
        property.AreaM2 = request.AreaM2;
        property.Bedrooms = request.Bedrooms;
        property.Suites = request.Suites;
        property.ParkingSpots = request.ParkingSpots;
        property.Type = request.Type;
        property.Purpose = request.Purpose;
        property.ImageUrl = request.ImageUrl;
        property.SourceUrl = request.SourceUrl;
        property.Features = request.Features;
        property.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        property.Agency = agency;
        return Ok(ToDto(property));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var property = await db.Properties.FindAsync(id);
        if (property is null) return NotFound();

        property.Active = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<Agency> GetOrCreateAgencyAsync(string name)
    {
        var agency = await db.Agencies.FirstOrDefaultAsync(a => a.Name == name);
        if (agency is not null) return agency;

        agency = new Agency { Name = name };
        db.Agencies.Add(agency);
        await db.SaveChangesAsync();
        return agency;
    }

    private static PropertyDto ToDto(Property p) => new(
        p.Id, p.Title, p.Neighborhood, p.City, p.Price, p.AreaM2, p.Bedrooms, p.Suites,
        p.ParkingSpots, p.Type.ToString(), p.Purpose.ToString(), p.ImageUrl, p.SourceUrl,
        p.Features, p.Agency?.Name ?? string.Empty);
}
