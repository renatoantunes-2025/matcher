using MatchR.Api.Models;

namespace MatchR.Api.ViewModels;

public class AdminViewModel
{
    public List<AccessRequest> PendingRequests { get; set; } = [];
    public int TotalActiveProperties { get; set; }
    public int AgencyCount { get; set; }
}
