using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController(IVideoService videoService,IDirectoryProvider directoryProvider) : ControllerBase
    {
        private string UploadFolder => directoryProvider.GetBaseUploadFolder();

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadVideoChunk([FromForm] IFormFile file, [FromForm] long fileSize)
        {
            var tempFilePath = Path.Combine(UploadFolder, file.FileName + ".part");

            await using (var stream = new FileStream(tempFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                await file.CopyToAsync(stream);
            }

            var fileInfo = new FileInfo(tempFilePath);

            if (fileInfo.Length >= fileSize)
            {
                string finalPath = Path.Combine(UploadFolder, file.FileName);
                System.IO.File.Move(tempFilePath, finalPath, true);

            }

            return Created();
        }

        [HttpGet("DRM/{videoId:guid}")]
        public async Task<IActionResult> GetDrm([FromRoute]Guid videoId)
        {
            var drmFilePath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "key.keyinfo");

            if (!System.IO.File.Exists(drmFilePath))
            {
                return NotFound("File not found.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(drmFilePath);
            return File(fileBytes, "application/octet-stream");
        }

    }
}