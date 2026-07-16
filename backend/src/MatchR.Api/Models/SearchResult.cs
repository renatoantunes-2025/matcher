namespace MatchR.Api.Models;

public class SearchResult
{
    public int Id { get; set; }
    public int SearchRequestId { get; set; }
    public SearchRequest? SearchRequest { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }

    public int Score { get; set; }
    public List<string> Reasons { get; set; } = [];
    public bool Selected { get; set; }
}
