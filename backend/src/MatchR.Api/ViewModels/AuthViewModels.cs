using System.ComponentModel.DataAnnotations;

namespace MatchR.Api.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}

public class AccessRequestViewModel
{
    [Required(ErrorMessage = "Informe o nome completo.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o CRECI.")]
    public string Creci { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
}
