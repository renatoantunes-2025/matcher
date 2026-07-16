using MatchR.Api.Models;

namespace MatchR.Api.Dtos;

public record PropertyDto(
    int Id,
    string Title,
    string Neighborhood,
    string City,
    decimal Price,
    decimal AreaM2,
    int Bedrooms,
    int Suites,
    int ParkingSpots,
    string Type,
    string Purpose,
    string? ImageUrl,
    string? SourceUrl,
    List<string> Features,
    string Agency);

public record PropertyUpsertRequest(
    string Title,
    string Neighborhood,
    string City,
    decimal Price,
    decimal AreaM2,
    int Bedrooms,
    int Suites,
    int ParkingSpots,
    PropertyType Type,
    PropertyPurpose Purpose,
    string? ImageUrl,
    string? SourceUrl,
    List<string> Features,
    string Agency);
