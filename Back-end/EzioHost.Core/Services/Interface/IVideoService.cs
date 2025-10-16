﻿using EzioHost.Domain.Entities;
using EzioHost.Shared.Events;
using System.Linq.Expressions;
using static EzioHost.Shared.Enums.VideoEnum;

namespace EzioHost.Core.Services.Interface
{
    public interface IVideoService
    {
        public event Action<VideoStreamAddedEvent> OnVideoStreamAdded;
        public event Action<VideoProcessDoneEvent> OnVideoProcessDone;

        Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null,
            Expression<Func<Video, object>>[]? includes = null);
        Task<Video?> GetVideoById(Guid videoId);
        Task<Video?> GetVideoUpscaleById(Guid videoId);
        Task<Video?> GetVideoToEncode();
        Task<Video> AddNewVideo(Video newVideo);
        Task<Video> UpdateVideo(Video updateVideo);
        Task EncodeVideo(Video inputVideo);
        Task DeleteVideo(Video deleteVideo);
        Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId);

        Task<VideoStream> CreateHlsVariantStream(string absoluteRawLocation, Video inputVideo, VideoResolution targetResolution);

        public int GetBandwidthForResolution(string resolution);
        public string GetResolutionDimensions(string resolution);

    }
}
