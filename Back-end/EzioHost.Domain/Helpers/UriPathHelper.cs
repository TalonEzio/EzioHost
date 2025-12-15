using EzioHost.Domain.Exceptions;

namespace EzioHost.Domain.Helpers;

/// <summary>
///     Helper class for normalizing file system paths to URI paths
/// </summary>
internal static class UriPathHelper
{
    private static readonly Uri DummyBaseUri = new("http://localhost/");

    /// <summary>
    ///     Normalizes a file system path to a URI path (forward slashes)
    /// </summary>
    /// <param name="path">The file system path to normalize</param>
    /// <param name="propertyName">The name of the property for error messages</param>
    /// <returns>Normalized URI path with forward slashes</returns>
    /// <exception cref="InvalidUriException">Thrown when the path is invalid</exception>
    public static string NormalizeUriPath(string? path, string propertyName)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        try
        {
            var cleanPath = path.TrimStart('/', '\\').TrimEnd('/', '\\');
            var finalUri = new Uri(DummyBaseUri, cleanPath);
            return finalUri.AbsolutePath.TrimStart('/');
        }
        catch (Exception)
        {
            throw new InvalidUriException($"Invalid {propertyName} uri");
        }
    }
}