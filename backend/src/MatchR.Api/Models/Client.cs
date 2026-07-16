namespace MatchR.Api.Models;

public enum ClientStatus
{
    Lead = 0,
    Cliente = 1,
    Parceiro = 2
}

public class Client
{
    public int Id { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.Lead;
    public string? Preferences { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public List<SearchRequest> Searches { get; set; } = [];
    public List<ShareEvent> ShareEvents { get; set; } = [];
}
