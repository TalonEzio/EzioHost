using Microsoft.JSInterop;

namespace EzioHost.WebApp.Client.Extensions;

public static class JsRuntimeExtension
{
    extension(IJSRuntime jsRuntime)
    {
        public ValueTask<string> GetOrigin()
        {
            return jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
        }

        public ValueTask<string> GetCurrentUrl()
        {
            return jsRuntime.InvokeAsync<string>("eval", "window.location.href");
        }

        public ValueTask ShowToast(string type, string message)
        {
            return jsRuntime.InvokeVoidAsync("showToast", type, message);
        }

        public ValueTask ShowSuccessToast(string message)
        {
            return jsRuntime.ShowToast("success", message);
        }

        public ValueTask ShowErrorToast(string message)
        {
            return jsRuntime.ShowToast("error", message);
        }

        public ValueTask ShowInfoToast(string message)
        {
            return jsRuntime.ShowToast("info", message);
        }

        public ValueTask ShowWarningToast(string message)
        {
            return jsRuntime.ShowToast("warning", message);
        }

        public ValueTask NavigateToAsync(string url)
        {
            return jsRuntime.InvokeVoidAsync("eval", $"window.location.href = '{url}';");
        }

        public void NavigateTo(string url)
        {
            jsRuntime.InvokeVoidAsync("eval", $"window.location.href = '{url}';").GetAwaiter().GetResult();
        }
    }
}