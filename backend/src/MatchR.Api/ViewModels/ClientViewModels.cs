using System.ComponentModel.DataAnnotations;
using MatchR.Api.Models;

namespace MatchR.Api.ViewModels;

public class ClientsIndexViewModel
{
    public List<Client> Clients { get; set; } = [];
}

public class ClientFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Informe o nome.")]
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.Lead;
    public string? Preferences { get; set; }
}

public class ClientDetailViewModel
{
    public Client Client { get; set; } = null!;
    public List<ShareEvent> Events { get; set; } = [];
}
