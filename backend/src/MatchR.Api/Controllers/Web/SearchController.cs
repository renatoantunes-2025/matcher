using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("busca")]
public class SearchController(MatchRDbContext db, IMatchingService matchingService) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? clientId, string? briefing)
    {
        ViewData["Title"] = "Nova busca";
        return View(new SearchFormViewModel
        {
            Clients = await db.Clients.Where(c => c.BrokerId == BrokerId).OrderBy(c => c.Name).ToListAsync(),
            Agencies = await db.Agencies.OrderBy(a => a.Name).ToListAsync(),
            ClientId = clientId,
            Briefing = briefing
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(SearchFormPost form)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == form.ClientId && c.BrokerId == BrokerId);
        if (client is null)
        {
            TempData["Toast"] = "Selecione um cliente válido para continuar.";
            return RedirectToAction(nameof(Index));
        }

        var search = new SearchRequest
        {
            ClientId = client.Id,
            BrokerId = BrokerId,
            Label = form.Label,
            BriefingText = form.BriefingText,
            Location = form.Location,
            Type = form.Type,
            Purpose = form.Purpose,
            AgencyId = form.AgencyId,
            PriceMin = form.PriceMinRange > 0 ? form.PriceMinRange * 1_000_000 : null,
            PriceMax = form.PriceMaxRange < 20 ? form.PriceMaxRange * 1_000_000 : null,
            MinArea = form.MinArea,
            Bedrooms = ParseOption(form.Dormitorios),
            Suites = ParseOption(form.Suites),
            ParkingSpots = ParseOption(form.Vagas),
            Features = form.Features ?? []
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
            Summary = string.IsNullOrWhiteSpace(form.Label) ? form.BriefingText : form.Label,
            ResultCount = matches.Count
        });

        await db.SaveChangesAsync();

        TempData["Toast"] = "Briefing interpretado. Resultados ordenados por compatibilidade.";
        return RedirectToAction("Index", "Results", new { id = search.Id });
    }

    private static int? ParseOption(string? value) =>
        string.IsNullOrEmpty(value) || value == "Qualquer" ? null : int.Parse(value.TrimEnd('+'));
}
