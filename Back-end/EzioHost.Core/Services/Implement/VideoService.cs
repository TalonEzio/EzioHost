using System.Drawing;
using System.Linq.Expressions;
using System.Text;
using EzioHost.Core.Providers;
using EzioHost.Core.Repositories;
using EzioHost.Core.Services.Interface;
using EzioHost.Core.UnitOfWorks;
using EzioHost.Domain.Entities;
using EzioHost.Domain.Events;
using EzioHost.Shared.Extensions;
using FFMpegCore;
using FFMpegCore.Enums;
using static EzioHost.Shared.Enums.VideoEnum;
using VideoStream = EzioHost.Domain.Entities.VideoStream;

namespace EzioHost.Core.Services.Implement
{
    public class VideoService(IVideoUnitOfWork videoUnitOfWork, IDirectoryProvider directoryProvider, IProtectService protectService) : IVideoService
    {

        private readonly string _webRootPath = directoryProvider.GetWebRootPath();

        private readonly IVideoRepository _videoRepository = videoUnitOfWork.VideoRepository;
        private readonly IVideoStreamRepository _videoStreamRepository = videoUnitOfWork.VideoStreamRepository;

        public Task<Video?> GetVideoToEncode()
        {
            return _videoRepository.GetVideoToEncode();
        }

        public event Action<VideoChangedEvent>? VideoChanged;

        public Task<IEnumerable<Video>> GetVideos(Expression<Func<Video, bool>>? expression = null, Expression<Func<Video, object>>[]? includes = null)
        {
            return _videoRepository.GetVideos(expression, includes);
        }

        public async Task<Video> AddNewVideo(Video newVideo)
        {
            var rawLocation = Path.Combine(_webRootPath, newVideo.RawLocation);
            var mediaInfo = await FFProbe.AnalyseAsync(rawLocation);

            var videoHeight = mediaInfo.VideoStreams[0].Height;

            newVideo.UpdateResolution(videoHeight);

            await _videoRepository.AddNewVideo(newVideo);

            VideoChanged?.Invoke(new VideoChangedEvent()
            {
                Instance = newVideo,
                ChangedType = VideoChangedType.Added
            });

            return newVideo;
        }

        public Task<Video?> GetVideoById(Guid videoId)
        {
            return _videoRepository.GetVideoById(id: videoId);
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
                await _videoRepository.UpdateVideo(inputVideo);


                var videoStreams = new List<VideoStream>();

                foreach (var videoResolution in inputVideo.Resolution.GetEnumsLessThanOrEqual())
                {
                    var newVideoStream = await CreateHlsVariantStream(absoluteRawLocation, inputVideo, videoResolution);
                    videoStreams.Add(newVideoStream);

                    _videoStreamRepository.Create(newVideoStream);
                    inputVideo.VideoStreams.Add(newVideoStream);
                }

                var m3U8Folder = new FileInfo(inputVideo.M3U8Location).Directory!.FullName;
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

                inputVideo.Status = VideoStatus.Ready;

                await videoUnitOfWork.VideoRepository.UpdateVideoForUnitOfWork(inputVideo);

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
            var segmentFolder = Path.Combine(_webRootPath, Path.GetDirectoryName(inputVideo.M3U8Location)!, targetResolution.GetDescription());
            if (!Directory.Exists(segmentFolder))
            {
                Directory.CreateDirectory(segmentFolder);
            }

            var segmentPath = Path.Combine(segmentFolder, $"{targetResolution.GetDescription()}_%05d.ts");
            var absoluteVideoStreamM3U8Location = Path.Combine(segmentFolder, $"{targetResolution.GetDescription()}.m3u8");

            var videoStream = new VideoStream()
            {
                Id = Guid.NewGuid(),
                Resolution = targetResolution,
                VideoId = inputVideo.Id,
                Video = inputVideo,
                Key = protectService.GenerateRandomKey(), // 🔑 Random key
                IV = protectService.GenerateRandomIv(),   // 🔄 Random IV
                M3U8Location = Path.GetRelativePath(_webRootPath, absoluteVideoStreamM3U8Location)
            };

            var targetHeight = (int)targetResolution;
            var targetWidth = (int)Math.Round(targetHeight * 16 / 9.0);
            if (targetWidth % 2 != 0) targetWidth++;

            var resolutionSize = new Size(targetWidth, targetHeight);

            var argumentProcessor = FFMpegArguments
                .FromFileInput(absoluteRawLocation)
                .OutputToFile(absoluteVideoStreamM3U8Location, true,
                    options => options
                        .WithVideoCodec("h264_nvenc")
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithVideoFilters(videoFilterOptions => videoFilterOptions.Scale(resolutionSize))
                        //.WithCustomArgument("-crf 18")
                        .WithCustomArgument("-force_key_frames \"expr:gte(t,n_forced*1)\"")
                        .WithCustomArgument("-f hls")
                        .WithCustomArgument("-hls_time 15")
                        .WithCustomArgument($"-hls_segment_filename \"{segmentPath}\"")
                        .WithCustomArgument("-hls_playlist_type vod")
                        //.WithCustomArgument("-hls_enc 1")// enable encrypt video
                        //.WithCustomArgument($"-hls_enc_key {videoStream.Key}") // 🔑 Key
                        //.WithCustomArgument($"-hls_enc_iv {videoStream.IV}")   // 🔄 IV
                        .WithAudibleEncryptionKeys(videoStream.Key,videoStream.IV)
                        .WithFastStart()
                );


            try
            {
                await argumentProcessor.ProcessAsynchronously();
                var m3U8Content = await File.ReadAllLinesAsync(absoluteVideoStreamM3U8Location);

                for (int i = 0; i < m3U8Content.Length; i++)
                {
                    if (m3U8Content[i].StartsWith("#EXT-X-KEY:"))
                    {
                        string line = m3U8Content[i];
                        int startIndex = line.IndexOf("URI=\"", StringComparison.Ordinal);
                        if (startIndex != -1)
                        {
                            startIndex += "URI=\"".Length;
                            int endIndex = line.IndexOf("\"", startIndex, StringComparison.Ordinal);
                            if (endIndex != -1)
                            {
                                string oldUri = line.Substring(startIndex, endIndex - startIndex);

                                string newUri = $"/api/video/drm/{videoStream.Id}";

                                m3U8Content[i] = line.Replace(oldUri, newUri);
                            }
                        }
                        break;
                    }
                }

                await File.WriteAllLinesAsync(absoluteVideoStreamM3U8Location, m3U8Content);
            }
            catch (Exception e)
            {
                Console.WriteLine($"FFMpeg processing error: {e.Message}");
            }


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

        public Task<Video?> GetVideoByVideoStreamId(Guid videoStreamId)
        {
            return _videoRepository.GetVideoByVideoStreamId(videoStreamId);
        }

    }
}
