using System.Drawing;
using System.Linq.Expressions;
using System.Text;
using EzioHost.Core.Extensions;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Events;
using FFMpegCore;
using FFMpegCore.Enums;
using static EzioHost.Domain.Enums.VideoEnum;
using VideoStream = EzioHost.Domain.Entities.VideoStream;

namespace EzioHost.Core.Services.Implement
{
    public class VideoService(IVideoUnitOfWork videoUnitOfWork, IDirectoryProvider directoryProvider) : IVideoService
    {

        private readonly string _webRootPath = directoryProvider.GetWebRootPath();

        private readonly IVideoRepository _videoRepository = videoUnitOfWork.VideoRepository;
        private readonly IVideoStreamRepository _videoStreamRepository = videoUnitOfWork.VideoStreamRepository;

        public event Action<VideoChangedEvent>? VideoChanged;

        public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null, Expression<Func<Video, object>>[]? includes = null)
        {
            return _videoRepository.GetVideos(expression, includes);
        }

        public async Task<Video> AddNewVideo(Video newVideo)
        {
            await _videoRepository.AddNewVideo(newVideo);

            VideoChanged?.Invoke(new VideoChangedEvent()
            {
                Instance = newVideo,
                ChangedType = VideoChangedType.Added
            });

            return newVideo;
        }

        public async Task<Video> UpdateVideo(Video updateVideo)
        {
            var video = await _videoRepository.UpdateVideo(updateVideo);
            VideoChanged?.Invoke(new VideoChangedEvent()
            {
                Instance = video,
                ChangedType = VideoChangedType.Edited
            });
            return video;
        }

        public async Task EncodeVideo(Video inputVideo)
        {
            //Absolute path
            var absoluteRawLocation = Path.Combine(_webRootPath, inputVideo.RawLocation);
            var absoluteM3U8Location = Path.Combine(_webRootPath, inputVideo.M3U8Location);

            try
            {
                await videoUnitOfWork.BeginTransactionAsync();

                inputVideo.Status = VideoStatus.Encoding;

                var m3U8Folder = new FileInfo(inputVideo.M3U8Location).Directory!.FullName;

                var videoStreams = new List<VideoStream>();

                foreach (var videoResolution in inputVideo.Resolution.GetEnumsLessThanOrEqual())
                {
                    var newVideoStream = await CreateHlsVariantStream(absoluteRawLocation, inputVideo, videoResolution);
                    videoStreams.Add(newVideoStream);

                    _videoStreamRepository.Create(newVideoStream);
                    inputVideo.VideoStreams.Add(newVideoStream);
                }

                if (!Directory.Exists(m3U8Folder))
                {
                    Directory.CreateDirectory(m3U8Folder);
                }

                var m3U8MergeFileStream = new FileStream(absoluteM3U8Location, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                var m3U8AllContentBuilder = new StringBuilder();
                m3U8AllContentBuilder.AppendLine("#EXTM3U");

                foreach (var videoStream in videoStreams)
                {
                    var currentResolution = videoStream.Resolution.GetDescription();
                    var filePath = Path.Combine(currentResolution, Path.GetFileName(videoStream.M3U8Location))
                        .Replace("\\", "/");

                    m3U8AllContentBuilder.AppendLine(
                        $"#EXT-X-STREAM-INF:BANDWIDTH={GetBandwidthForResolution(currentResolution)},RESOLUTION={GetResolutionDimensions(currentResolution)}");
                    m3U8AllContentBuilder.AppendLine(filePath);
                }

                await m3U8MergeFileStream.WriteAsync(Encoding.UTF8.GetBytes(m3U8AllContentBuilder.ToString()));
                m3U8MergeFileStream.Close();

                await videoUnitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await videoUnitOfWork.RollbackTransactionAsync();
            }
        }

        private static int GetBandwidthForResolution(string resolution)
        {
            return resolution switch
            {
                "360p" => 800000,
                "480p" => 1400000,
                "720p" => 2800000,
                "1080p" => 5000000,
                "1440p" => 8000000,
                "2160p" => 15000000,
                _ => 1000000
            };
        }

        private static string GetResolutionDimensions(string resolution)
        {
            switch (resolution)
            {
                case "360p": return "640x360";
                case "480p": return "854x480";
                case "720p": return "1280x720";
                case "1080p": return "1920x1080";
                case "1440p": return "2560x1440";
                case "2160p": return "3840x2160";
                default: return "1920x1080";
            }
        }

        private async Task<VideoStream> CreateHlsVariantStream(string absoluteRawLocation, Video inputVideo, VideoResolution targetResolution)
        {

            var segmentFolder = Path.Combine(Path.GetDirectoryName(inputVideo.M3U8Location)!, targetResolution.GetDescription());
            if (!Directory.Exists(segmentFolder))
            {
                Directory.CreateDirectory(segmentFolder);
            }

            var segmentPath = Path.Combine(segmentFolder, $"{targetResolution.GetDescription()}_%05d.ts");
            var absoluteVideoStreamM3U8Location = Path.Combine(segmentFolder, $"{inputVideo.Title}_{targetResolution.GetDescription()}.m3u8");

            var videoStream = new VideoStream()
            {
                Id = Guid.NewGuid(),
                Resolution = targetResolution,
                VideoId = inputVideo.Id,
                Video = inputVideo,
                M3U8Location = Path.GetRelativePath(_webRootPath, absoluteVideoStreamM3U8Location)
            };

            var resolutionSize = new Size((int)targetResolution * 16 / 9, (int)targetResolution);

            var argumentProcessor = FFMpegArguments
                .FromFileInput(absoluteRawLocation)
                .OutputToFile(absoluteVideoStreamM3U8Location, true,
                    options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithVideoFilters(videoFilterOptions => videoFilterOptions.Scale(resolutionSize))
                    .WithCustomArgument("-crf 18")
                    .WithCustomArgument("-preset ultrafast")
                    .WithCustomArgument("-force_key_frames \"expr:gte(t,n_forced*1)\"")
                    .WithCustomArgument("-f hls")
                    .WithCustomArgument("-hls_time 10")
                    .WithCustomArgument($"-hls_segment_filename \"{segmentPath}\"")
                    .WithCustomArgument("-hls_playlist_type vod")
                    .WithFastStart()
                );
            await argumentProcessor.ProcessAsynchronously();
            return videoStream;
        }


        public async Task DeleteVideo(Video deleteVideo)
        {
            await _videoRepository.DeleteVideo(deleteVideo);

            VideoChanged?.Invoke(new VideoChangedEvent()
            {
                Instance = deleteVideo,
                ChangedType = VideoChangedType.Deleted
            });

        }
    }
}
