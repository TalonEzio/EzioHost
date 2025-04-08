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

        public static ValueTask ShowToast(this IJSRuntime js, string type, string message)
            => js.InvokeVoidAsync("showToast", type, message);

        public static ValueTask ShowSuccessToast(this IJSRuntime js, string message)
            => js.ShowToast("success", message);

        public static ValueTask ShowErrorToast(this IJSRuntime js, string message)
            => js.ShowToast("error", message);

        public static ValueTask ShowInfoToast(this IJSRuntime js, string message)
            => js.ShowToast("info", message);

        public static ValueTask ShowWarningToast(this IJSRuntime js, string message)
            => js.ShowToast("warning", message);

        public static ValueTask NavigateToAsync(this IJSRuntime jsRuntime, string url)
        {
            return jsRuntime.InvokeVoidAsync("eval", $"window.location.href = '{url}';");
        }
        public static void NavigateTo(this IJSRuntime jsRuntime, string url)
        {
            jsRuntime.InvokeVoidAsync("eval", $"window.location.href = '{url}';").GetAwaiter().GetResult();
        }

    }
}
