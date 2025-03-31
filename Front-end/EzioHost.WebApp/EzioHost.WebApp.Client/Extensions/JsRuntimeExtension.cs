using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Extensions
{
    public static class JsRuntimeExtension
    {
        public static ValueTask<string> GetOrigin(this IJSRuntime jsRuntime)
        {
            return jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
        }
        
        public static ValueTask<string> GetCurrentUrl(this IJSRuntime jsRuntime)
        {
            return jsRuntime.InvokeAsync<string>("eval", "window.location.href");
        }
    }
}
