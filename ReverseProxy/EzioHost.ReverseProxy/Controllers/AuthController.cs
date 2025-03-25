using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace EzioHost.ReverseProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IOptionsMonitor<AppSettings> appSettings) : ControllerBase
    {

        [HttpGet("/login")]
        public async Task Login(string? returnUrl)
        {
            if (HttpContext.User.Identity is { IsAuthenticated: true })
            {
                Response.Redirect(Url.Content("~/"));
            }
            else
            {
                returnUrl ??= "/";
                var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
                await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties()
                    {
                        RedirectUri = redirectUri
                    });
            }
        }

        [HttpGet("/logout")]
        public async Task Logout(string? returnUrl = null)
        {
            returnUrl ??= "~/";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Redirect(Url.Content(returnUrl));
        }

        [HttpGet("/user")]
        public IActionResult GetUser()
        {
            if (HttpContext.User.Identity is { IsAuthenticated: false }) return Unauthorized();

            var claims = HttpContext.User.Claims.ToList();

            var nameClaim = claims.FirstOrDefault(c => c.Type == appSettings.CurrentValue.OpenIdConnect.NameClaimType);
            if (nameClaim != null)
            {
                claims.Remove(nameClaim);
                claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
            }

            var roleClaims = claims.Where(c => c.Type == appSettings.CurrentValue.OpenIdConnect.RoleClaimType).ToList();
            foreach (var roleClaim in roleClaims)
            {
                claims.Remove(roleClaim);
                var roles = roleClaim.Value.Split(',');
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                }
            }

            return Ok(claims.Select(ClaimDto.ConvertFromClaim));
        }


    }
}
