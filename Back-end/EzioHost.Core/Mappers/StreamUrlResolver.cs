using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace EzioHost.Core.Mappers;

public class StreamUrlResolver(IConfiguration configuration) : IMemberValueResolver<VideoStream, VideoStreamDto, string?, string>
{
    private readonly string _baseRoute = configuration["ManifestStreamSettings:BaseUrl"] ?? string.Empty;

    public string Resolve(VideoStream source, VideoStreamDto destination, string? sourceMember, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(sourceMember))
            return string.Empty;

        return $"{_baseRoute}/{source.VideoId}/{(int)source.Resolution}".Replace("//", "/");
    }
}