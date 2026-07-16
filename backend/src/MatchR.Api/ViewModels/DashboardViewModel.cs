using MatchR.Api.Models;

namespace MatchR.Api.ViewModels;

public class DashboardViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public List<Client> Clients { get; set; } = [];
    public int ActiveClients { get; set; }
    public int SearchesThisMonth { get; set; }
    public int FavoritedProperties { get; set; }
    public int SharesSent { get; set; }
    public List<Client> RecentClients { get; set; } = [];
    public List<ShareEvent> RecentActivity { get; set; } = [];
}
