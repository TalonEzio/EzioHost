using EzioHost.Shared.Constants;
using EzioHost.Shared.Models;
using Refit;

namespace EzioHost.WebApp.Client.Services;

public interface IUploadApi
{
    [Post("/api/Upload/init")]
    Task<ApiResponse<UploadInfoDto>> InitUpload([Body] UploadInfoDto uploadInfo);

    [Post("/api/Upload/chunk/{uploadId}")]
    [Multipart]
    Task UploadChunk(
        Guid uploadId,
        [AliasAs(FormFieldNames.ChunkFile)] StreamPart chunkFile);

    [Delete("/api/Upload/{uploadId}")]
    Task CancelUpload(Guid uploadId);
}