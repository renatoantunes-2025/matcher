using MatchR.Api.Models;

namespace MatchR.Api.Dtos;

public record SearchCreateRequest(
    int ClientId,
    string? Label,
    string BriefingText,
    string? Location,
    PropertyType? Type,
    PropertyPurpose? Purpose,
    int? AgencyId,
    decimal? PriceMin,
    decimal? PriceMax,
    decimal? MinArea,
    int? Bedrooms,
    int? Suites,
    int? ParkingSpots,
    List<string>? Features);

public record SearchResultItemDto(
    int PropertyId,
    PropertyDto Property,
    int Score,
    List<string> Reasons,
    bool Selected);

public record SearchResponseDto(
    int SearchId,
    string ClientName,
    List<SearchResultItemDto> Results);

public record SelectionUpdateRequest(int PropertyId, bool Selected);

public record ShareRequest(string? Message);
