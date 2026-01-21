using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Enums;
using EzioHost.Shared.Models;
using EzioHost.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EzioHost.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UploadController(
    IFileUploadService fileUploadService,
    ILogger<UploadController> logger) : ControllerBase
{
    [HttpPost("init")]
    public async Task<IActionResult> InitUpload([FromBody] UploadInfoDto fileInfo)
    {
        var userId = HttpContext.User.UserId;
        logger.LogInformation(
            "Initializing file upload. FileName: {FileName}, FileSize: {FileSize} bytes, ContentType: {ContentType}, UserId: {UserId}",
            fileInfo.FileName,
            fileInfo.FileSize,
            fileInfo.ContentType,
            userId);

        try
        {
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
                    logger.LogInformation(
                        "Resuming existing upload. FileUploadId: {FileUploadId}, FileName: {FileName}, UserId: {UserId}",
                        existingUpload.Id,
                        fileInfo.FileName,
                        userId);
                    return Ok(existingUpload);
                case { IsCompleted: true }:
                {
                    logger.LogInformation(
                        "Copying completed file. ExistingFileUploadId: {ExistingFileUploadId}, FileName: {FileName}, UserId: {UserId}",
                        existingUpload.Id,
                        fileInfo.FileName,
                        userId);
                    var copyFileUpload = await fileUploadService.CopyCompletedFile(existingUpload, userId);
                    logger.LogInformation(
                        "File copy created. NewFileUploadId: {NewFileUploadId}, UserId: {UserId}",
                        copyFileUpload.Id,
                        userId);
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

            logger.LogInformation(
                "New file upload initialized. FileUploadId: {FileUploadId}, FileName: {FileName}, UserId: {UserId}",
                newFileUpload.Id,
                fileInfo.FileName,
                userId);

            return Created(string.Empty, newFileUpload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error initializing file upload. FileName: {FileName}, UserId: {UserId}",
                fileInfo.FileName,
                userId);
            throw;
        }
    }

    [HttpPost("chunk/{uploadId:guid}")]
    public async Task<IActionResult> UploadChunk([FromRoute] Guid uploadId, [FromForm] IFormFile chunkFile)
    {
        var userId = HttpContext.User.UserId;
        logger.LogDebug(
            "Uploading chunk. FileUploadId: {FileUploadId}, ChunkSize: {ChunkSize} bytes, UserId: {UserId}",
            uploadId,
            chunkFile.Length,
            userId);

        try
        {
            var fileUpload = await fileUploadService.GetFileUploadById(uploadId);

            if (fileUpload == null)
            {
                logger.LogWarning("File upload not found for chunk upload. FileUploadId: {FileUploadId}, UserId: {UserId}", uploadId, userId);
                return NotFound();
            }

            if (fileUpload.IsCompleted)
            {
                logger.LogWarning(
                    "Attempted to upload chunk to completed upload. FileUploadId: {FileUploadId}, UserId: {UserId}",
                    uploadId,
                    userId);
                return BadRequest();
            }

            var chunkFileStream = chunkFile.OpenReadStream();
            var status = await fileUploadService.UploadChunk(fileUpload, chunkFileStream);

            logger.LogDebug(
                "Chunk upload completed. FileUploadId: {FileUploadId}, Status: {Status}, Progress: {ReceivedBytes}/{FileSize} bytes, UserId: {UserId}",
                uploadId,
                status,
                fileUpload.ReceivedBytes,
                fileUpload.FileSize,
                userId);

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
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error uploading chunk. FileUploadId: {FileUploadId}, UserId: {UserId}",
                uploadId,
                userId);
            throw;
        }
    }

    [HttpDelete("{uploadId:guid}")]
    public async Task<IActionResult> CancelUpload([FromRoute] Guid uploadId)
    {
        var userId = HttpContext.User.UserId;
        logger.LogInformation("Canceling file upload. FileUploadId: {FileUploadId}, UserId: {UserId}", uploadId, userId);

        try
        {
            var fileUpload = await fileUploadService.GetFileUploadById(uploadId);
            if (fileUpload == null)
            {
                logger.LogWarning("File upload not found for cancellation. FileUploadId: {FileUploadId}, UserId: {UserId}", uploadId, userId);
                return NotFound();
            }

            if (fileUpload.IsCompleted)
            {
                logger.LogWarning(
                    "Attempted to cancel completed upload. FileUploadId: {FileUploadId}, UserId: {UserId}",
                    uploadId,
                    userId);
                return BadRequest();
            }

            await fileUploadService.UpdateFileUpload(fileUpload);
            logger.LogInformation("File upload canceled. FileUploadId: {FileUploadId}, UserId: {UserId}", uploadId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error canceling file upload. FileUploadId: {FileUploadId}, UserId: {UserId}", uploadId, userId);
            throw;
        }
    }
}