using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EzioHost.Core.Providers;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace EzioHost.Infrastructure.Storage.CloudFlare.Services.Implement;

public class R2StorageService(
    IAmazonS3 s3Client,
    ISettingProvider settingsProvider,
    ILogger<R2StorageService> logger) : IStorageService
{
    private StorageSettings StorageSettings => settingsProvider.GetStorageSettings();

    public async Task<string> UploadFileAsync(string localFilePath, string key, string contentType)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Uploading file to R2 storage. Key: {Key}, ContentType: {ContentType}, LocalPath: {LocalPath}",
            key,
            contentType,
            localFilePath);

        try
        {
            var fileInfo = new FileInfo(localFilePath);
            if (!fileInfo.Exists)
            {
                logger.LogError("Local file not found for upload. Key: {Key}, LocalPath: {LocalPath}", key, localFilePath);
                throw new FileNotFoundException($"Local file not found: {localFilePath}");
            }

            logger.LogDebug(
                "File info. Key: {Key}, FileSize: {FileSize} bytes",
                key,
                fileInfo.Length);

            var putRequest = new PutObjectRequest
            {
                BucketName = StorageSettings.BucketName,
                Key = key,
                FilePath = localFilePath,
                ContentType = contentType,
                DisablePayloadSigning = true
            };

            await s3Client.PutObjectAsync(putRequest);

            stopwatch.Stop();
            var publicUrl = $"{StorageSettings.PublicDomain}/{key}";

            logger.LogInformation(
                "File uploaded successfully to R2. Key: {Key}, PublicUrl: {PublicUrl}, FileSize: {FileSize} bytes, Duration: {DurationMs}ms",
                key,
                publicUrl,
                fileInfo.Length,
                stopwatch.ElapsedMilliseconds);

            return publicUrl;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error uploading file to R2. Key: {Key}, LocalPath: {LocalPath}, Duration: {DurationMs}ms",
                key,
                localFilePath,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<string> UploadLargeFileAsync(string localFilePath, string key, string contentType)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Uploading large file to R2 storage. Key: {Key}, ContentType: {ContentType}, LocalPath: {LocalPath}",
            key,
            contentType,
            localFilePath);

        try
        {
            var fileInfo = new FileInfo(localFilePath);
            if (!fileInfo.Exists)
            {
                logger.LogError("Local file not found for large upload. Key: {Key}, LocalPath: {LocalPath}", key, localFilePath);
                throw new FileNotFoundException($"Local file not found: {localFilePath}");
            }

            logger.LogInformation(
                "Large file info. Key: {Key}, FileSize: {FileSize} bytes ({FileSizeMB:F2} MB)",
                key,
                fileInfo.Length,
                fileInfo.Length / (1024.0 * 1024.0));

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

            // Track upload progress
            var lastLoggedPercent = 0;
            uploadRequest.UploadProgressEvent += (s, e) =>
            {
                var currentPercent = (int)e.PercentDone;
                if (currentPercent >= lastLoggedPercent + 25) // Log every 25%
                {
                    logger.LogDebug(
                        "Large file upload progress. Key: {Key}, Progress: {PercentDone}%, Transferred: {TransferredBytes}/{TotalBytes} bytes",
                        key,
                        e.PercentDone,
                        e.TransferredBytes,
                        e.TotalBytes);
                    lastLoggedPercent = currentPercent;
                }
            };

            await utility.UploadAsync(uploadRequest);

            stopwatch.Stop();
            var publicUrl = $"{StorageSettings.PublicDomain}/{key}";

            logger.LogInformation(
                "Large file uploaded successfully to R2. Key: {Key}, PublicUrl: {PublicUrl}, FileSize: {FileSize} bytes ({FileSizeMB:F2} MB), Duration: {DurationMs}ms ({DurationSeconds:F1}s)",
                key,
                publicUrl,
                fileInfo.Length,
                fileInfo.Length / (1024.0 * 1024.0),
                stopwatch.ElapsedMilliseconds,
                stopwatch.Elapsed.TotalSeconds);

            return publicUrl;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex,
                "Error uploading large file to R2. Key: {Key}, LocalPath: {LocalPath}, Duration: {DurationMs}ms",
                key,
                localFilePath,
                stopwatch.ElapsedMilliseconds);
            throw new Exception($"Upload large file failed: {ex.Message}", ex);
        }
    }
}