using System.Security.Claims;

namespace EzioHost.WebAPI.Middlewares
{
    public class BindingUserIdMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.User is { Identity.IsAuthenticated: true })
            {
                if (context.Request.Headers.ContainsKey("X-USER-ID"))
                {
                    var parse = Guid.TryParse(context.Request.Headers["X-USER-ID"].ToString(), out var userId);
                    if (parse)
                    {
                        var claims = context.User.Claims.ToList();

                        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Reverse Proxy"));
                    }
                }

            }
            await next(context);
        }
    }
}
