using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("clientes")]
public class ClientsController(MatchRDbContext db) : WebControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clients = await db.Clients.Include(c => c.Searches)
            .Where(c => c.BrokerId == BrokerId)
            .OrderByDescending(c => c.LastActivityAt)
            .ToListAsync();

        ViewData["Title"] = "Clientes";
        return View(new ClientsIndexViewModel { Clients = clients });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        var events = await db.ShareEvents.Include(e => e.Client)
            .Where(e => e.ClientId == id && e.BrokerId == BrokerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        ViewData["Title"] = client.Name;
        return View(new ClientDetailViewModel { Client = client, Events = events });
    }

    [HttpGet("novo")]
    public IActionResult Create()
    {
        ViewData["Title"] = "Novo cliente";
        return View("Form", new ClientFormViewModel());
    }

    [HttpPost("novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Novo cliente";
            return View("Form", model);
        }

        db.Clients.Add(new Client
        {
            BrokerId = BrokerId,
            Name = model.Name,
            Phone = model.Phone,
            Email = model.Email,
            Status = model.Status,
            Preferences = model.Preferences
        });
        await db.SaveChangesAsync();

        TempData["Toast"] = "Cliente salvo com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/editar")]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        ViewData["Title"] = "Editar cliente";
        return View("Form", new ClientFormViewModel
        {
            Id = client.Id,
            Name = client.Name,
            Phone = client.Phone,
            Email = client.Email,
            Status = client.Status,
            Preferences = client.Preferences
        });
    }

    [HttpPost("{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClientFormViewModel model)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Editar cliente";
            return View("Form", model);
        }

        client.Name = model.Name;
        client.Phone = model.Phone;
        client.Email = model.Email;
        client.Status = model.Status;
        client.Preferences = model.Preferences;
        client.LastActivityAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Toast"] = "Cliente salvo com sucesso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:int}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.BrokerId == BrokerId);
        if (client is null) return NotFound();

        db.Clients.Remove(client);
        await db.SaveChangesAsync();

        TempData["Toast"] = "Cliente removido.";
        return RedirectToAction(nameof(Index));
    }
}
