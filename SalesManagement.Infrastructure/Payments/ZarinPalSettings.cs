namespace SalesManagement.Infrastructure.Payments
{
    public class ZarinPalSettings
    {
        public const string SectionName = "ZarinPal";
        public string MerchantId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://payment.zarinpal.com";
        public string CallbackBaseUrl { get; set; } = string.Empty;
        public string FrontendResultUrl { get; set; } = string.Empty;
    }
}
