using static EzioHost.Shared.Private.Endpoints.Configuration;
namespace EzioHost.Shared.Private.Endpoints
{
    public static class BaseUrl
    {
        public static string ReverseProxyUrl => Root[nameof(ReverseProxyUrl)]!;
        public static string WebApiUrl => Root[nameof(WebApiUrl)]!;
        public static string FrontendUrl => Root[nameof(FrontendUrl)]!;
    }

    public static class PrefixCommon
    {
        public static string WebApiPrefixStaticFile => Root[nameof(WebApiPrefixStaticFile)]!;
    }
}
