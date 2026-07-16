namespace MatchR.Api.Dtos;

public record DashboardStatsDto(
    int ActiveClients,
    int SearchesThisMonth,
    int FavoritedProperties,
    int SharesSent);

public record RecentClientDto(int Id, string Name, ClientDto Client);

public record ActivityItemDto(string Description, DateTime CreatedAt);
