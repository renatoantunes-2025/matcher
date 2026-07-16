using System.Text;
using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("resultados")]
public class ResultsController(MatchRDbContext db) : WebControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var search = await db.SearchRequests.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == id && s.BrokerId == BrokerId);
        if (search is null) return NotFound();

        var results = await db.SearchResults.Include(r => r.Property).ThenInclude(p => p!.Agency)
            .Where(r => r.SearchRequestId == id)
            .OrderByDescending(r => r.Score)
            .ToListAsync();

        var favoriteIds = (await db.Favorites.Where(f => f.BrokerId == BrokerId).Select(f => f.PropertyId).ToListAsync()).ToHashSet();

        ViewData["Title"] = "Resultados";
        return View(new ResultsViewModel { Search = search, Results = results, FavoritePropertyIds = favoriteIds });
    }

    [HttpPost("{id:int}/selecionar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSelection(int id, int propertyId, bool selected)
    {
        var result = await db.SearchResults
            .FirstOrDefaultAsync(r => r.SearchRequestId == id && r.PropertyId == propertyId
                && r.SearchRequest!.BrokerId == BrokerId);
        if (result is null) return NotFound();

        result.Selected = selected;
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("{id:int}/favoritar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id, int propertyId)
    {
        var existing = await db.Favorites.FirstOrDefaultAsync(f => f.BrokerId == BrokerId && f.PropertyId == propertyId);
        if (existing is not null)
        {
            db.Favorites.Remove(existing);
            TempData["Toast"] = "Imóvel removido dos favoritos.";
        }
        else
        {
            db.Favorites.Add(new Favorite { BrokerId = BrokerId, PropertyId = propertyId });
            TempData["Toast"] = "Imóvel adicionado aos favoritos.";
        }
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost("{id:int}/compartilhar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Share(int id, ShareFormPost form)
    {
        var search = await db.SearchRequests.Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == id && s.BrokerId == BrokerId);
        if (search is null) return NotFound();

        var selected = await db.SearchResults.Include(r => r.Property)
            .Where(r => r.SearchRequestId == id && r.Selected)
            .ToListAsync();

        db.ShareEvents.Add(new ShareEvent
        {
            ClientId = search.ClientId,
            BrokerId = BrokerId,
            SearchRequestId = search.Id,
            Type = ShareEventType.WhatsAppShare,
            Summary = $"Seleção de imóveis para {search.Client!.Name}",
            ResultCount = selected.Count
        });
        search.Client.LastActivityAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var text = new StringBuilder(form.Message ?? $"Olá, {search.Client.Name}! Separei estes imóveis com maior compatibilidade com o seu perfil. Veja as opções abaixo:");
        foreach (var r in selected)
        {
            text.Append("\n\n").Append(r.Property!.Title).Append(" – ").Append(Helpers.FormatPrice(r.Property.Price));
            if (!string.IsNullOrEmpty(r.Property.SourceUrl)) text.Append('\n').Append(r.Property.SourceUrl);
        }

        var waUrl = "https://wa.me/?text=" + Uri.EscapeDataString(text.ToString());
        return View("ShareRedirect", waUrl);
    }
}
