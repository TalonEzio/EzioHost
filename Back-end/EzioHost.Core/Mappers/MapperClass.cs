using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using EzioHost.Shared.Private.Settings;

namespace EzioHost.Core.Mappers;

public class MapperClass : Profile
{
    private static readonly string PrefixStatic = PrefixCommon.WebApiPrefixStaticFile;

    private static readonly Uri DumpUri = new("http://localhost/");

    public MapperClass()
    {
        CreateMap<Video, VideoDto>()
            .ForMember(x =>
                    x.M3U8Location,
                otp => otp.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.M3U8Location))
            )
            .ReverseMap();

        CreateMap<VideoStream, VideoStreamDto>()
            .ForMember(x =>
                    x.M3U8Location,
                otp => otp.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.M3U8Location))
            )
            .ReverseMap();

        CreateMap<OnnxModel, OnnxModelDto>()
            .ForMember(x =>
                    x.DemoInput,
                cfg => cfg.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.DemoInput))
            )
            .ForMember(x =>
                    x.DemoOutput,
                cfg => cfg.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.DemoOutput))
            )
            .ReverseMap();

        CreateMap<OnnxModel, UpscaleDemoResponseDto>()
            .ForMember(x =>
                    x.ModelId,
                cfg => cfg.MapFrom(x => x.Id)
            )
            .ForMember(x =>
                    x.DemoInput,
                cfg => cfg.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.DemoInput))
            )
            .ForMember(x =>
                    x.DemoOutput,
                cfg => cfg.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.DemoOutput))
            );

        CreateMap<VideoUpscale, VideoUpscaleDto>()
            .ForMember(x =>
                    x.OutputLocation,
                otp => otp.MapFrom(x => AddPrefixIfNotNullOrEmpty(x.OutputLocation))
            )
            .ReverseMap();
    }

    private string AddPrefixIfNotNullOrEmpty(string? path)
    {
        if (string.IsNullOrEmpty(path)) return path ?? string.Empty;

        var prefix = PrefixStatic.TrimEnd('/', '\\');
        var cleanPath = path.TrimStart('/', '\\');
        var combinedPath = $"{prefix}/{cleanPath}";
        try
        {
            var finalUri = new Uri(DumpUri, combinedPath);
            return finalUri.AbsolutePath;
        }
        catch (UriFormatException)
        {
            return combinedPath;
        }
    }
}