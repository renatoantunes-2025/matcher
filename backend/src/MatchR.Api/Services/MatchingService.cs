using MatchR.Api.Models;

namespace MatchR.Api.Services;

public record ScoredProperty(Property Property, int Score, List<string> Reasons);

public interface IMatchingService
{
    List<ScoredProperty> Match(SearchRequest search, IEnumerable<Property> candidates);
}

/// <summary>
/// Rule-based matching engine. Weights mirror the admin screen: location 30%,
/// type 20%, price 15%, remaining 35% split across area/bedrooms/suites/parking/features.
/// Free-text briefing fills in any filter the broker left blank.
/// </summary>
public class MatchingService : IMatchingService
{
    private const int LocationWeight = 30;
    private const int TypeWeight = 20;
    private const int PriceWeight = 15;
    private const int AreaWeight = 7;
    private const int BedroomsWeight = 7;
    private const int SuitesWeight = 7;
    private const int ParkingWeight = 7;
    private const int FeaturesWeight = 7;

    public List<ScoredProperty> Match(SearchRequest search, IEnumerable<Property> candidates)
    {
        var hints = BriefingParser.Parse(search.BriefingText);

        var priceMax = search.PriceMax ?? hints.PriceMax;
        var minArea = search.MinArea ?? hints.MinArea;
        var bedrooms = search.Bedrooms ?? hints.Bedrooms;
        var suites = search.Suites ?? hints.Suites;
        var parking = search.ParkingSpots ?? hints.ParkingSpots;
        var features = search.Features.Count > 0 ? search.Features : hints.Features;
        var locationTerms = (search.Location ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToList();

        var scored = candidates
            .Where(p => p.Active)
            .Where(p => search.Purpose is null || p.Purpose == search.Purpose)
            .Where(p => search.AgencyId is null || p.AgencyId == search.AgencyId)
            .Select(p => Score(p, search, locationTerms, priceMax, minArea, bedrooms, suites, parking, features))
            .OrderByDescending(r => r.Score)
            .ToList();

        return scored;
    }

    private static ScoredProperty Score(
        Property p,
        SearchRequest search,
        List<string> locationTerms,
        decimal? priceMax,
        decimal? minArea,
        int? bedrooms,
        int? suites,
        int? parking,
        List<string> features)
    {
        var score = 0;
        var reasons = new List<string>();

        // Location
        if (locationTerms.Count == 0)
        {
            score += LocationWeight;
        }
        else
        {
            var propertyLocation = $"{p.Neighborhood} {p.City}".ToLowerInvariant();
            if (locationTerms.Any(propertyLocation.Contains))
            {
                score += LocationWeight;
                reasons.Add("Localização ideal");
            }
        }

        // Type
        if (search.Type is null || search.Type == p.Type)
        {
            score += TypeWeight;
        }

        // Price
        if (priceMax is null)
        {
            score += PriceWeight;
        }
        else if (p.Price <= priceMax)
        {
            score += PriceWeight;
        }
        else
        {
            var overBy = (p.Price - priceMax.Value) / priceMax.Value;
            var partial = (decimal)PriceWeight * Math.Max(0, 1 - overBy * 4);
            score += (int)Math.Round(partial);
        }

        // Area
        if (minArea is null || p.AreaM2 >= minArea)
        {
            score += AreaWeight;
        }

        // Bedrooms
        if (bedrooms is null || p.Bedrooms >= bedrooms)
        {
            score += BedroomsWeight;
            if (bedrooms is not null) reasons.Add($"{p.Bedrooms} dormitórios");
        }

        // Suites
        if (suites is null || p.Suites >= suites)
        {
            score += SuitesWeight;
        }

        // Parking
        if (parking is null || p.ParkingSpots >= parking)
        {
            score += ParkingWeight;
        }

        // Features
        if (features.Count == 0)
        {
            score += FeaturesWeight;
        }
        else
        {
            var propertyFeatures = p.Features.Select(f => f.ToLowerInvariant()).ToList();
            var matchedFeatures = features
                .Where(f => propertyFeatures.Any(pf => pf.Contains(f.ToLowerInvariant())))
                .ToList();
            var ratio = (decimal)matchedFeatures.Count / features.Count;
            score += (int)Math.Round(FeaturesWeight * ratio);
            if (matchedFeatures.Count > 0) reasons.Add(matchedFeatures.First());
        }

        if (reasons.Count == 0 && score >= 70) reasons.Add("Boa aderência ao briefing");

        return new ScoredProperty(p, Math.Clamp(score, 0, 100), reasons.Take(3).ToList());
    }
}
