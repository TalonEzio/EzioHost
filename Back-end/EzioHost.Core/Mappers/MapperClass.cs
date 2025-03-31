using AutoMapper;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;

namespace EzioHost.Core.Mappers
{
    public class MapperClass : Profile
    {
        public MapperClass()
        {
            CreateMap<Video, VideoDto>()
                .ForMember(x =>
                    x.M3U8Location, 
                    otp =>
                        otp.MapFrom(x => Path.Combine("/static", x.M3U8Location).Replace("\\","/"))
                        )
                .ReverseMap();
            CreateMap<VideoStream, VideoStreamDto>()
                .ForMember(x => 
                x.M3U8Location, 
                    otp => 
                        otp.MapFrom(x => Path.Combine("/static", x.M3U8Location).Replace("\\", "/")))
                .ReverseMap();
        }
    }
}
