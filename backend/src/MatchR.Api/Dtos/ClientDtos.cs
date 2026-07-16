using MatchR.Api.Models;

namespace MatchR.Api.Dtos;

public record ClientDto(
    int Id,
    string Name,
    string? Phone,
    string? Email,
    string Status,
    string? Preferences,
    DateTime LastActivityAt,
    int SearchCount);

public record ClientUpsertRequest(
    string Name,
    string? Phone,
    string? Email,
    ClientStatus Status,
    string? Preferences);
