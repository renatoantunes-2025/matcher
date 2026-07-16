namespace MatchR.Api.Models;

public class Favorite
{
    public int Id { get; set; }
    public int BrokerId { get; set; }
    public Broker? Broker { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
