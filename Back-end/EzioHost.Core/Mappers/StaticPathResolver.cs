using AutoMapper;
using Microsoft.Extensions.Configuration;

namespace EzioHost.Core.Mappers;

public class StaticPathResolver(IConfiguration configuration) : IMemberValueResolver<object, object, string?, string>
{
    private static readonly Uri DumpUri = new("http://localhost/");
    private readonly string _prefixStatic = configuration["StaticFileSettings:WebApiPrefixStaticFile"] ?? "/static";


    public string Resolve(object source, object destination, string? sourceMember, string destMember,
        ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember)) return sourceMember ?? string.Empty;

        var prefix = _prefixStatic.TrimEnd('/', '\\');
        var cleanPath = sourceMember.TrimStart('/', '\\');
        var combinedPath = $"{prefix}/{cleanPath}";

        try
        {
            var finalUri = new Uri(DumpUri, combinedPath);
            return finalUri.AbsolutePath.Replace("//", "/");
        }
        catch (UriFormatException)
        {
            return combinedPath.Replace("//", "/");
        }
    }
}