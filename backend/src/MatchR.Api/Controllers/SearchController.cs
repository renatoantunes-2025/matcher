using MatchR.Api.Data;
using MatchR.Api.Dtos;
using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

[Route("api/searches")]
public class SearchController(MatchRDbContext db, IMatchingService matchingService) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SearchResponseDto>> Create(SearchCreateRequest request)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId && c.BrokerId == BrokerId);
        if (client is null) return NotFound(new { message = "Cliente não encontrado." });

        var search = new SearchRequest
        {
            ClientId = client.Id,
            BrokerId = BrokerId,
            Label = request.Label,
            BriefingText = request.BriefingText,
            Location = request.Location,
            Type = request.Type,
            Purpose = request.Purpose,
            AgencyId = request.AgencyId,
            PriceMin = request.PriceMin,
            PriceMax = request.PriceMax,
            MinArea = request.MinArea,
            Bedrooms = request.Bedrooms,
            Suites = request.Suites,
            ParkingSpots = request.ParkingSpots,
            Features = request.Features ?? []
        };

        var candidates = await db.Properties.Include(p => p.Agency).Where(p => p.Active).ToListAsync();
        var matches = matchingService.Match(search, candidates).Take(20).ToList();

        search.Results = matches.Select((m, i) => new SearchResult
        {
            PropertyId = m.Property.Id,
            Score = m.Score,
            Reasons = m.Reasons,
            Selected = i < 2
        }).ToList();

        db.SearchRequests.Add(search);

        client.LastActivityAt = DateTime.UtcNow;
        db.ShareEvents.Add(new ShareEvent
        {
            ClientId = client.Id,
            BrokerId = BrokerId,
            SearchRequestId = search.Id,
            Type = ShareEventType.Search,
            Summary = string.IsNullOrWhiteSpace(request.Label) ? request.BriefingText : request.Label,
            ResultCount = matches.Count
        });

        await db.SaveChangesAsync();

        return Ok(await BuildResponseAsync(search.Id, client.Name));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SearchResponseDto>> GetById(int id)
    {
        var search = await db.SearchRequests
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == id && s.BrokerId == BrokerId);
        if (search is null) return NotFound();

        return Ok(await BuildResponseAsync(id, search.Client!.Name));
    }

    [HttpPatch("{id:int}/selection")]
    public async Task<ActionResult<SearchResponseDto>> UpdateSelection(int id, SelectionUpdateRequest request)
    {
        var search = await db.SearchRequests
            .Include(s => s.Client)
            .Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == id && s.BrokerId == BrokerId);
        if (search is null) return NotFound();

        var result = search.Results.FirstOrDefault(r => r.PropertyId == request.PropertyId);
        if (result is null) return NotFound(new { message = "Imóvel não faz parte desta busca." });

        result.Selected = request.Selected;
        await db.SaveChangesAsync();

        return Ok(await BuildResponseAsync(id, search.Client!.Name));
    }

    [HttpPost("{id:int}/share")]
    public async Task<IActionResult> Share(int id, ShareRequest request)
    {
        var search = await db.SearchRequests
            .Include(s => s.Client)
            .Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == id && s.BrokerId == BrokerId);
        if (search is null) return NotFound();

        var selectedCount = search.Results.Count(r => r.Selected);

        db.ShareEvents.Add(new ShareEvent
        {
            ClientId = search.ClientId,
            BrokerId = BrokerId,
            SearchRequestId = search.Id,
            Type = ShareEventType.WhatsAppShare,
            Summary = $"Seleção de imóveis para {search.Client!.Name}",
            ResultCount = selectedCount
        });

        search.Client.LastActivityAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Compartilhamento registrado.", resultCount = selectedCount });
    }

    private async Task<SearchResponseDto> BuildResponseAsync(int searchId, string clientName)
    {
        var results = await db.SearchResults
            .Include(r => r.Property).ThenInclude(p => p!.Agency)
            .Where(r => r.SearchRequestId == searchId)
            .OrderByDescending(r => r.Score)
            .ToListAsync();

        var items = results.Select(r => new SearchResultItemDto(
            r.PropertyId,
            new PropertyDto(
                r.Property!.Id, r.Property.Title, r.Property.Neighborhood, r.Property.City,
                r.Property.Price, r.Property.AreaM2, r.Property.Bedrooms, r.Property.Suites,
                r.Property.ParkingSpots, r.Property.Type.ToString(), r.Property.Purpose.ToString(),
                r.Property.ImageUrl, r.Property.SourceUrl, r.Property.Features,
                r.Property.Agency?.Name ?? string.Empty),
            r.Score,
            r.Reasons,
            r.Selected)).ToList();

        return new SearchResponseDto(searchId, clientName, items);
    }
}
