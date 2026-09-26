namespace SalesManagement.Application.Features.Payments.VerifyPayment
{
    public sealed record VerifyPaymentResult(bool IsSuccess, string TrackingCode);
}
