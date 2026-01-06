using static EzioHost.Shared.Private.Settings.Configuration;

namespace EzioHost.Shared.Private.Settings;

public static class PrefixCommon
{
    public static string WebApiPrefixStaticFile => Root[nameof(WebApiPrefixStaticFile)]!;
}