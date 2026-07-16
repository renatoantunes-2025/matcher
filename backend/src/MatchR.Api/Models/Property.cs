namespace MatchR.Api.Models;

public enum PropertyType
{
    Casa = 0,
    CasaEmCondominio = 1,
    Apartamento = 2,
    Cobertura = 3,
    CasaDeVila = 4,
    Duplex = 5
}

public enum PropertyPurpose
{
    Compra = 0,
    Locacao = 1
}

public class Property
{
    public int Id { get; set; }
    public int AgencyId { get; set; }
    public Agency? Agency { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal AreaM2 { get; set; }
    public int Bedrooms { get; set; }
    public int Suites { get; set; }
    public int ParkingSpots { get; set; }
    public PropertyType Type { get; set; } = PropertyType.Apartamento;
    public PropertyPurpose Purpose { get; set; } = PropertyPurpose.Compra;
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public List<string> Features { get; set; } = [];
    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SearchResult> SearchResults { get; set; } = [];
    public List<Favorite> Favorites { get; set; } = [];
}
