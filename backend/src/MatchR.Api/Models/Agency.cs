namespace MatchR.Api.Models;

public class Agency
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Property> Properties { get; set; } = [];
}
