using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Settings;

namespace EzioHost.Infrastructure.Storage.CloudFlare.Services.Implement;

public class R2StorageService(IAmazonS3 s3Client, ISettingProvider settingsProvider) : IStorageService
{
    private StorageSettings StorageSettings => settingsProvider.GetStorageSettings();

    public async Task<string> UploadFileAsync(string localFilePath, string key, string contentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = StorageSettings.BucketName,
            Key = key,
            FilePath = localFilePath,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await s3Client.PutObjectAsync(putRequest);
        return $"{StorageSettings.PublicDomain}/{key}";
    }

    public async Task<string> UploadLargeFileAsync(string localFilePath, string key, string contentType)
    {
        try
        {
            var config = new TransferUtilityConfig
            {
                ConcurrentServiceRequests = 5,
                MinSizeBeforePartUpload = 20 * 1024 * 1024
            };

            using var utility = new TransferUtility(s3Client, config);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = StorageSettings.BucketName,
                Key = key,
                FilePath = localFilePath,
                ContentType = contentType,
                PartSize = 10 * 1024 * 1024,
                DisablePayloadSigning = true,
                CannedACL = S3CannedACL.PublicRead
            };

            //uploadRequest.UploadProgressEvent += (s, e) => Console.WriteLine($"Uploaded: {e.PercentDone}%");

            await utility.UploadAsync(uploadRequest);
            return $"{StorageSettings.PublicDomain}/{key}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Upload large file failed: {ex.Message}", ex);
        }
    }
}