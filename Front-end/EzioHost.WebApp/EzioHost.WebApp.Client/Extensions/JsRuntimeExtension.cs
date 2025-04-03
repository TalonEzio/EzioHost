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

        public static ValueTask ShowSuccessToast(this IJSRuntime jsRuntime, string message)
        {
            return jsRuntime.InvokeVoidAsync("showSuccessToast", message);

        }
        public static ValueTask ShowErrorToast(this IJSRuntime jsRuntime, string message)
        {
            return jsRuntime.InvokeVoidAsync("showErrorToast", message);
        }
        public static ValueTask NavigateTo(this IJSRuntime jsRuntime, string url)
        {
            return jsRuntime.InvokeVoidAsync("eval", $"window.location.href = '{url}';");
        }

    }
}
