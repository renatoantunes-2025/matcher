using MatchR.Api.Data;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("favoritos")]
public class FavoritesController(MatchRDbContext db) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var favorites = await db.Favorites.Include(f => f.Property).ThenInclude(p => p!.Agency)
            .Where(f => f.BrokerId == BrokerId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        ViewData["Title"] = "Favoritos";
        return View(favorites.Select(f => f.Property!).ToList());
    }

    [HttpPost("{propertyId:int}/remover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int propertyId)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.BrokerId == BrokerId && f.PropertyId == propertyId);
        if (favorite is not null)
        {
            db.Favorites.Remove(favorite);
            await db.SaveChangesAsync();
            TempData["Toast"] = "Imóvel removido dos favoritos.";
        }
        return RedirectToAction(nameof(Index));
    }
}
