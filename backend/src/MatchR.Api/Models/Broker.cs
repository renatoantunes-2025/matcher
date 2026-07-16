namespace MatchR.Api.Models;

public enum BrokerRole
{
    Broker = 0,
    Admin = 1
}

public enum BrokerStatus
{
    Pending = 0,
    Active = 1,
    Rejected = 2
}

public class Broker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Creci { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public BrokerRole Role { get; set; } = BrokerRole.Broker;
    public BrokerStatus Status { get; set; } = BrokerStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Client> Clients { get; set; } = [];
}
