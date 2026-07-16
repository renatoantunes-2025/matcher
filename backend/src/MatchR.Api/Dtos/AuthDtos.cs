namespace MatchR.Api.Dtos;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Name, string Email, string Role);

public record AccessRequestDto(string Name, string Creci, string Email, string? Phone);
