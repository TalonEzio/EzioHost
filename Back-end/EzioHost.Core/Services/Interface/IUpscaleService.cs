using EzioHost.Domain.Entities;

namespace EzioHost.Core.Services.Interface
{
    public interface IUpscaleService
    {

        Task UpscaleImage(OnnxModel model, string inputPath, string outputPath);

        Task UpscaleVideo(OnnxModel model, Video video);
    }
}
