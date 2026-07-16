using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers;

public class ImportController(MatchRDbContext db, IImportService importService) : ApiControllerBase
{
    private static readonly string[] AllowedExtensions = [".xlsx", ".csv"];

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { message = "Arquivo vazio." });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Envie um arquivo .xlsx ou .csv." });
        }

        var batch = new ImportBatch { BrokerId = BrokerId, FileName = file.FileName };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        try
        {
            await using var stream = file.OpenReadStream();
            var outcome = await importService.ImportAsync(stream, file.FileName, ct);

            batch.RecordCount = outcome.RecordCount;
            batch.Status = outcome.RecordCount == 0 && outcome.Errors.Count > 0
                ? ImportStatus.Failed
                : ImportStatus.Completed;
            batch.ErrorMessage = outcome.Errors.Count > 0 ? string.Join("; ", outcome.Errors.Take(10)) : null;
        }
        catch (Exception ex)
        {
            batch.Status = ImportStatus.Failed;
            batch.ErrorMessage = ex.Message;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            batch.Id,
            batch.FileName,
            batch.RecordCount,
            Status = batch.Status.ToString(),
            batch.ErrorMessage
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory()
    {
        var batches = await db.ImportBatches
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .ToListAsync();

        var dtos = batches.Select(b => new
        {
            b.Id,
            b.FileName,
            b.CreatedAt,
            b.RecordCount,
            Status = b.Status.ToString()
        });

        return Ok(dtos);
    }
}
