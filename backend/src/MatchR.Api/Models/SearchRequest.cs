namespace MatchR.Api.Models;

public class SearchRequest
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }

    public string? Label { get; set; }
    public string BriefingText { get; set; } = string.Empty;

    public string? Location { get; set; }
    public PropertyType? Type { get; set; }
    public PropertyPurpose? Purpose { get; set; }
    public int? AgencyId { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public decimal? MinArea { get; set; }
    public int? Bedrooms { get; set; }
    public int? Suites { get; set; }
    public int? ParkingSpots { get; set; }
    public List<string> Features { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SearchResult> Results { get; set; } = [];
}
