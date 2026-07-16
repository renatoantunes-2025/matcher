using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[Route("importacao")]
[Authorize(Roles = nameof(BrokerRole.Admin))]
public class ImportController(MatchRDbContext db, IImportService importService) : WebControllerBase
{
    private static readonly string[] AllowedExtensions = [".xlsx", ".csv"];

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Importação";
        var history = await db.ImportBatches.OrderByDescending(b => b.CreatedAt).Take(20).ToListAsync();
        return View(history);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Toast"] = "Selecione um arquivo antes de enviar.";
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            TempData["Toast"] = "Envie um arquivo .xlsx ou .csv.";
            return RedirectToAction(nameof(Index));
        }

        var batch = new ImportBatch { BrokerId = BrokerId, FileName = file.FileName };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        try
        {
            await using var stream = file.OpenReadStream();
            var outcome = await importService.ImportAsync(stream, file.FileName, ct);

            batch.RecordCount = outcome.RecordCount;
            batch.Status = outcome.RecordCount == 0 && outcome.Errors.Count > 0 ? ImportStatus.Failed : ImportStatus.Completed;
            batch.ErrorMessage = outcome.Errors.Count > 0 ? string.Join("; ", outcome.Errors.Take(10)) : null;
        }
        catch (Exception ex)
        {
            batch.Status = ImportStatus.Failed;
            batch.ErrorMessage = ex.Message;
        }

        await db.SaveChangesAsync(ct);

        TempData["Toast"] = batch.Status == ImportStatus.Failed
            ? $"Falha na importação: {batch.ErrorMessage}"
            : $"Planilha validada com sucesso. {batch.RecordCount} registros importados.";
        return RedirectToAction(nameof(Index));
    }
}
