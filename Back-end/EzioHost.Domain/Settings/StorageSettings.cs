namespace EzioHost.Domain.Settings
{
    public class StorageSettings
    {
        public string ServiceUrl { get; set; } = string.Empty;
        public string PublicDomain { get; set; } = string.Empty;

        public string AccountId { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;

        public string BucketName { get; set; } = string.Empty;
    }
}
