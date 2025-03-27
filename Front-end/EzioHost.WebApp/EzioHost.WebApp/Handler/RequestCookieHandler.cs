﻿namespace EzioHost.WebApp.Handler
{
    public class RequestCookieHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) return await base.SendAsync(request, CancellationToken.None);
            
            if (context.Request.Cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", context.Request.Cookies.Select(c => $"{c.Key}={c.Value}")) + "; a=b";
                request.Headers.Add("Cookie", cookieHeader);
            }

            return await base.SendAsync(request, CancellationToken.None);
        }
    }
}
