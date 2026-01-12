using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using Microsoft.Extensions.Configuration;

namespace EzioHost.Core.Mappers;

public class SubtitleUrlResolver(IConfiguration configuration) : IMemberValueResolver<VideoSubtitle, VideoSubtitleDto, object, string>
{
    private readonly string _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "/api";

    public string Resolve(VideoSubtitle source, VideoSubtitleDto destination, object sourceMember, string destMember, ResolutionContext context)
    {
        var baseUrl = _apiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/VideoSubtitle/File/{source.Id}";
    }
}
