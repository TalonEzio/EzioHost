﻿using static EzioHost.Shared.Common.Configuration;
namespace EzioHost.Shared.Common
{
    public static class BaseUrlCommon
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
