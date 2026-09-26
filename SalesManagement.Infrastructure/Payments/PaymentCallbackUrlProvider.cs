using Microsoft.Extensions.Options;
using SalesManagement.Application.Common.Interfaces;

namespace SalesManagement.Infrastructure.Payments
{
    public class PaymentCallbackUrlProvider(IOptions<ZarinPalSettings> settings) : IPaymentCallbackUrlProvider
    {
        private readonly ZarinPalSettings _settings = settings.Value;

        public string Build(Guid paymentId) =>
            $"{_settings.CallbackBaseUrl.TrimEnd('/')}/api/payments/callback?paymentId={paymentId}";
    }
}
