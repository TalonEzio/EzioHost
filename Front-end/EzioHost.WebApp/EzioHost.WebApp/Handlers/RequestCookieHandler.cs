namespace EzioHost.WebApp.Handlers;

public class RequestCookieHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
        {
#if DEBUG
            return await base.SendAsync(request, CancellationToken.None);
#else
            return await base.SendAsync(request, cancellationToken);
#endif
        }

        if (context.Request.Cookies.Count > 0)
        {
            var cookieHeader = string.Join("; ", context.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

#if DEBUG
        return await base.SendAsync(request, CancellationToken.None);
#else
            return await base.SendAsync(request, cancellationToken);
#endif
    }
}