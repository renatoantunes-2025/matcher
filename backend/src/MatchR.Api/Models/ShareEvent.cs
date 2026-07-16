namespace MatchR.Api.Models;

public enum ShareEventType
{
    Search = 0,
    WhatsAppShare = 1
}

public class ShareEvent
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }
    public int? SearchRequestId { get; set; }
    public SearchRequest? SearchRequest { get; set; }

    public ShareEventType Type { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
