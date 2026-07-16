using System.Security.Claims;
using MatchR.Api.Data;
using MatchR.Api.Models;
using MatchR.Api.Services;
using MatchR.Api.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Controllers.Web;

[AllowAnonymous]
public class HomeController(MatchRDbContext db, IAuthService authService) : Controller
{
    [HttpGet("/")]
    public IActionResult Landing() => View();

    [HttpGet("/entrar")]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel());
    }

    [HttpPost("/entrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var broker = await db.Brokers.FirstOrDefaultAsync(b => b.Email == model.Email);
        if (broker is null || !authService.VerifyPassword(model.Password, broker.PasswordHash) || broker.Status != BrokerStatus.Active)
        {
            model.Error = "E-mail ou senha inválidos, ou cadastro ainda não aprovado.";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, broker.Id.ToString()),
            new(ClaimTypes.Email, broker.Email),
            new("name", broker.Name),
            new(ClaimTypes.Role, broker.Role.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost("/sair")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Landing));
    }

    [HttpPost("/solicitar-acesso")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAccess(AccessRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AccessRequestError"] = "Preencha nome, CRECI e e-mail corretamente.";
            return RedirectToAction(nameof(Landing));
        }

        db.AccessRequests.Add(new AccessRequest
        {
            Name = model.Name,
            Creci = model.Creci,
            Email = model.Email,
            Phone = model.Phone
        });
        await db.SaveChangesAsync();

        TempData["AccessRequestSent"] = true;
        return RedirectToAction(nameof(Landing));
    }
}
