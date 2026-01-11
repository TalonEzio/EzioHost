using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Mappers;

public class MapperClass : Profile
{
    public MapperClass()
    {
        CreateMap<Video, VideoDto>()
            .ForMember(dest => dest.M3U8Location,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.M3U8Location))
            .ForMember(dest => dest.Thumbnail,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.Thumbnail))
            .ReverseMap();

        CreateMap<VideoStream, VideoStreamDto>()
            .ForMember(dest => dest.M3U8Location,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.M3U8Location))
            .ReverseMap();

        CreateMap<OnnxModel, OnnxModelDto>()
            .ForMember(dest => dest.DemoInput,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.DemoInput))
            .ForMember(dest => dest.DemoOutput,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.DemoOutput))
            .ReverseMap();

        CreateMap<OnnxModel, UpscaleDemoResponseDto>()
            .ForMember(dest => dest.ModelId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DemoInput,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.DemoInput))
            .ForMember(dest => dest.DemoOutput,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.DemoOutput));

        CreateMap<VideoUpscale, VideoUpscaleDto>()
            .ForMember(dest => dest.OutputLocation,
                opt => opt.MapFrom<StaticPathResolver, string?>(src => src.OutputLocation))
            .ReverseMap();
    }
}