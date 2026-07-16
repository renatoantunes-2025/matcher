using MatchR.Api.Models;

namespace MatchR.Api.ViewModels;

public class SearchFormViewModel
{
    public List<Client> Clients { get; set; } = [];
    public List<Agency> Agencies { get; set; } = [];
    public int? ClientId { get; set; }
    public string? Briefing { get; set; }
}

public class SearchFormPost
{
    public int ClientId { get; set; }
    public string? Label { get; set; }
    public string BriefingText { get; set; } = string.Empty;
    public string? Location { get; set; }
    public PropertyType? Type { get; set; }
    public PropertyPurpose? Purpose { get; set; }
    public int? AgencyId { get; set; }
    public decimal PriceMinRange { get; set; }
    public decimal PriceMaxRange { get; set; } = 20;
    public decimal? MinArea { get; set; }
    public string? Dormitorios { get; set; }
    public string? Suites { get; set; }
    public string? Vagas { get; set; }
    public List<string>? Features { get; set; }
}

public class ResultsViewModel
{
    public SearchRequest Search { get; set; } = null!;
    public List<SearchResult> Results { get; set; } = [];
    public HashSet<int> FavoritePropertyIds { get; set; } = [];
}

public class ShareFormPost
{
    public string? Message { get; set; }
}
