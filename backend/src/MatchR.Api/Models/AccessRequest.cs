namespace MatchR.Api.Models;

public enum AccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class AccessRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Creci { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
