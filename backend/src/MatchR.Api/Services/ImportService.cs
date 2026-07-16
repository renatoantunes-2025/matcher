using System.Globalization;
using ClosedXML.Excel;
using MatchR.Api.Data;
using MatchR.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Services;

public record ImportOutcome(int RecordCount, List<string> Errors);

public interface IImportService
{
    Task<ImportOutcome> ImportAsync(Stream fileStream, string fileName, CancellationToken ct);
}

/// <summary>
/// Parses inventory spreadsheets (.xlsx or .csv) with columns:
/// Titulo, Bairro, Cidade, Preco, AreaM2, Dormitorios, Suites, Vagas, Tipo, Finalidade,
/// Imobiliaria, ImagemUrl, LinkOrigem, Caracteristicas (separated by ";").
/// Upserts by matching Titulo + Bairro.
/// </summary>
public class ImportService(MatchRDbContext db) : IImportService
{
    public async Task<ImportOutcome> ImportAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var rows = fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsv(fileStream)
            : ParseXlsx(fileStream);

        var errors = new List<string>();
        var count = 0;

        foreach (var (row, index) in rows.Select((r, i) => (r, i)))
        {
            try
            {
                await UpsertPropertyAsync(row, ct);
                count++;
            }
            catch (Exception ex)
            {
                errors.Add($"Linha {index + 2}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);
        return new ImportOutcome(count, errors);
    }

    private async Task UpsertPropertyAsync(Dictionary<string, string> row, CancellationToken ct)
    {
        var title = Get(row, "Titulo");
        var neighborhood = Get(row, "Bairro");
        var agencyName = Get(row, "Imobiliaria");

        var agency = await db.Agencies.FirstOrDefaultAsync(a => a.Name == agencyName, ct);
        if (agency is null)
        {
            agency = new Agency { Name = agencyName };
            db.Agencies.Add(agency);
            await db.SaveChangesAsync(ct);
        }

        var existing = await db.Properties.FirstOrDefaultAsync(
            p => p.Title == title && p.Neighborhood == neighborhood, ct);

        var property = existing ?? new Property();
        property.Title = title;
        property.Neighborhood = neighborhood;
        property.City = Get(row, "Cidade");
        property.Price = ParseDecimal(Get(row, "Preco"));
        property.AreaM2 = ParseDecimal(Get(row, "AreaM2"));
        property.Bedrooms = ParseInt(Get(row, "Dormitorios"));
        property.Suites = ParseInt(Get(row, "Suites"));
        property.ParkingSpots = ParseInt(Get(row, "Vagas"));
        property.Type = ParseEnum<PropertyType>(Get(row, "Tipo"));
        property.Purpose = Get(row, "Finalidade").Contains("loca", StringComparison.OrdinalIgnoreCase)
            ? PropertyPurpose.Locacao : PropertyPurpose.Compra;
        property.AgencyId = agency.Id;
        property.ImageUrl = Get(row, "ImagemUrl");
        property.SourceUrl = Get(row, "LinkOrigem");
        property.Features = Get(row, "Caracteristicas")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        property.Active = true;
        property.UpdatedAt = DateTime.UtcNow;

        if (existing is null) db.Properties.Add(property);
    }

    private static string Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value.Replace("R$", "").Replace(".", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result : 0;

    private static int ParseInt(string value) => int.TryParse(value, out var result) ? result : 0;

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value.Replace(" ", ""), true, out var result) ? result : default;

    private static List<Dictionary<string, string>> ParseXlsx(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var headerRow = sheet.Row(1);
        var headers = headerRow.CellsUsed().Select(c => c.GetString().Trim()).ToList();

        var rows = new List<Dictionary<string, string>>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= lastRow; r++)
        {
            var dict = new Dictionary<string, string>();
            for (var c = 0; c < headers.Count; c++)
            {
                dict[headers[c]] = sheet.Cell(r, c + 1).GetString();
            }
            if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v))) rows.Add(dict);
        }
        return rows;
    }

    private static List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);

        if (lines.Count == 0) return [];
        var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();

        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(',');
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Count && i < values.Length; i++)
            {
                dict[headers[i]] = values[i].Trim();
            }
            rows.Add(dict);
        }
        return rows;
    }
}
