using System.Globalization;
using System.Text.RegularExpressions;
using MatchR.Api.Models;

namespace MatchR.Api.Services;

/// <summary>
/// Extracts structured hints from the free-text briefing (rule-based, no LLM) to
/// fill in filters the broker left blank. Explicit filters always take priority.
/// </summary>
public static partial class BriefingParser
{
    private static readonly string[] KnownFeatures =
    [
        "academia", "piscina aquecida", "piscina", "lavanderia", "quadra de tênis",
        "chuveiro a gás", "garden", "ar condicionado", "varanda gourmet", "varanda",
        "rua silenciosa", "sol da manhã", "vista livre", "home-office", "home office",
        "cozinha americana", "moderno", "contemporâneo", "neoclássico"
    ];

    public static BriefingHints Parse(string text)
    {
        var lower = (text ?? string.Empty).ToLowerInvariant();

        decimal? priceMax = null;
        var priceMatch = MoneyRegex().Match(lower);
        if (priceMatch.Success)
        {
            var value = decimal.Parse(priceMatch.Groups["value"].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            priceMax = priceMatch.Groups["unit"].Value.StartsWith("mil") ? value * 1000 : value * 1_000_000;
        }

        decimal? minArea = null;
        var areaMatch = AreaRegex().Match(lower);
        if (areaMatch.Success)
        {
            minArea = decimal.Parse(areaMatch.Groups["value"].Value, CultureInfo.InvariantCulture);
        }

        int? bedrooms = null;
        var bedroomsMatch = BedroomsRegex().Match(lower);
        if (bedroomsMatch.Success)
        {
            bedrooms = int.Parse(bedroomsMatch.Groups["value"].Value);
        }

        int? suites = null;
        var suitesMatch = SuitesRegex().Match(lower);
        if (suitesMatch.Success)
        {
            suites = int.Parse(suitesMatch.Groups["value"].Value);
        }

        int? parking = null;
        var parkingMatch = ParkingRegex().Match(lower);
        if (parkingMatch.Success)
        {
            parking = int.Parse(parkingMatch.Groups["value"].Value);
        }

        var features = KnownFeatures.Where(f => lower.Contains(f)).Distinct().ToList();

        return new BriefingHints(priceMax, minArea, bedrooms, suites, parking, features);
    }

    [GeneratedRegex(@"(?:r\$\s*)?(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>milh(?:ão|ao|ões|oes)|mil)")]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"(?<value>\d+)\s*m(?:²|2)")]
    private static partial Regex AreaRegex();

    [GeneratedRegex(@"(?<value>\d+)\s*dormit[óo]rios?")]
    private static partial Regex BedroomsRegex();

    [GeneratedRegex(@"(?<value>\d+)\s*su[íi]tes?")]
    private static partial Regex SuitesRegex();

    [GeneratedRegex(@"(?<value>\d+)\s*vagas?")]
    private static partial Regex ParkingRegex();
}

public record BriefingHints(
    decimal? PriceMax,
    decimal? MinArea,
    int? Bedrooms,
    int? Suites,
    int? ParkingSpots,
    List<string> Features);
