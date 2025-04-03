using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Common;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Mappers
{
    public class MapperClass : Profile
    {
        private const string PrefixStatic = $"/{PrefixConstants.WebApiPrefixStaticFile}";

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
        }

        private string AddPrefixIfNotNullOrEmpty(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path ?? string.Empty;
            }
            return Path.Combine(PrefixStatic, path).Replace("\\", "/");
        }
    }
}
