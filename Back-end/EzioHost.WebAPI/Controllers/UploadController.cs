using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UploadController(IFileUploadService fileUploadService) : ControllerBase
{
    [HttpPost("init")]
    public async Task<IActionResult> InitUpload([FromBody] UploadInfoDto fileInfo)
    {
        var userId = HttpContext.User.UserId;

        var existingUpload = await fileUploadService
            .GetFileUploadByCondition(u =>
                u.FileName == fileInfo.FileName
                && u.FileSize == fileInfo.FileSize
                && u.Checksum == fileInfo.Checksum
                && u.CreatedBy == userId
            );

        switch (existingUpload)
        {
            case { IsCompleted: false }:
                return Ok(existingUpload);
            case { IsCompleted: true }:
                {
                    var copyFileUpload = await fileUploadService.CopyCompletedFile(existingUpload, userId);
                    return Created(string.Empty, copyFileUpload);
                }
        }

        var newFileUpload = new FileUpload
        {
            FileName = fileInfo.FileName,
            FileSize = fileInfo.FileSize,
            ContentType = fileInfo.ContentType,
            Checksum = fileInfo.Checksum,
            CreatedBy = userId
        };

        await fileUploadService.AddFileUpload(newFileUpload);

        return Created(string.Empty, newFileUpload);
    }

    [HttpPost("chunk/{uploadId:guid}")]
    public async Task<IActionResult> UploadChunk([FromRoute] Guid uploadId, [FromForm] IFormFile chunkFile)
    {
        var fileUpload = await fileUploadService.GetFileUploadById(uploadId);

        if (fileUpload == null) return NotFound();

        if (fileUpload.IsCompleted) return BadRequest();

        var chunkFileStream = chunkFile.OpenReadStream();
        var status = await fileUploadService.UploadChunk(fileUpload, chunkFileStream);

        return status switch
        {
            VideoEnum.FileUploadStatus.Completed => Created(),
            VideoEnum.FileUploadStatus.InProgress => Accepted(),
            VideoEnum.FileUploadStatus.Failed => BadRequest(),
            VideoEnum.FileUploadStatus.Pending => NoContent(),
            VideoEnum.FileUploadStatus.Canceled => NoContent(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [HttpDelete("{uploadId:guid}")]
    public async Task<IActionResult> CancelUpload([FromRoute] Guid uploadId)
    {
        var fileUpload = await fileUploadService.GetFileUploadById(uploadId);
        if (fileUpload == null) return NotFound();
        if (fileUpload.IsCompleted) return BadRequest();
        await fileUploadService.UpdateFileUpload(fileUpload);
        return NoContent();
    }
}