using System.Security.Claims;
using MatchR.Api.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatchR.Api.Controllers.Web;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
public abstract class WebControllerBase : Controller
{
    protected int BrokerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string BrokerName => User.FindFirstValue("name") ?? string.Empty;
    protected bool IsAdmin => User.IsInRole(nameof(BrokerRole.Admin));
}
