using System.Security.Claims;
using EzioHost.ReverseProxy.Extensions;
using EzioHost.ReverseProxy.Startup;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EzioHost.ReverseProxy.Controllers;

[ApiController]
public class AuthController(IOptionsMonitor<AppSettings> appSettings) : ControllerBase
{
    [HttpGet("/login")]
    public async Task Login([FromQuery] string? returnUrl)
    {
        returnUrl ??= "/";
        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        if (HttpContext.User.Identity is { IsAuthenticated: true })
            Response.Redirect(returnUrl);
        else
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = redirectUri
                });
    }

    [HttpGet("/logout")]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        returnUrl ??= "~/";

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        return Redirect(returnUrl);
    }

    [HttpGet("/user")]
    [Authorize]
    public IActionResult GetUser()
    {
        var claims = HttpContext.User.Claims.ToList();

        var nameClaim = claims.FirstOrDefault(c => c.Type == appSettings.CurrentValue.OpenIdConnect.NameClaimType);
        if (nameClaim != null)
        {
            claims.Remove(nameClaim);
            claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
        }

        var roleClaims =
            claims.Where(c => c.Type == appSettings.CurrentValue.OpenIdConnect.RoleClaimType).ToList(); //Roles
        foreach (var roleClaim in roleClaims)
        {
            claims.Remove(roleClaim);
            var roles = roleClaim.Value.Split(',');
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.Trim())));
        }

        return Ok(claims.Select(ClaimDto.ConvertFromClaim));
    }

    [HttpGet("/access-token")]
    public async Task<IActionResult> GetAccessToken()
    {
        if (HttpContext.User.Identity is { IsAuthenticated: false } or not ClaimsIdentity) return Unauthorized();

        var oidcToken = await HttpContext.GetDownstreamAccessTokenAsync();
        return Ok(oidcToken);
    }
}