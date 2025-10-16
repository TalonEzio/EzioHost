﻿using EzioHost.Domain.Entities;
using System.Linq.Expressions;

namespace EzioHost.Core.Repositories
{
    public interface IVideoRepository
    {
        Task<Video> AddNewVideo(Video newVideo);
        Task<Video?> GetVideoById(Guid id);
        Task<Video> UpdateVideo(Video updateVideo);
        Task<Video> UpdateVideoForUnitOfWork(Video updateVideo);
        Task DeleteVideo(Video deleteVideo);
        Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null, Expression<Func<Video, object>>[]? includes = null);

        Task<Video?> GetVideoToEncode();

        Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId);

        Task<Video?> GetVideoUpscaleById(Guid videoId);

    }
}
