namespace EzioHost.WebApp
{
    public class AppSettings
    {
        public PaypalSettings Paypal { get; set; } = new PaypalSettings();
    }

    public class PaypalSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        public string SdkUrl => $"https://www.sandbox.paypal.com/sdk/js?client-id={ClientId}";
    }
}
