using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface ISubtitleTranscribeApi
{
    [Post("/api/SubtitleTranscribe/{videoId}")]
    Task CreateTranscribeRequest(Guid videoId, [Body] CreateTranscribeRequestDto request);

    public class CreateTranscribeRequestDto
    {
        public string Language { get; set; } = string.Empty;
    }
}
