namespace SalesManagement.Application.Features.Payments.RequestPayment
{
    public sealed record RequestPaymentResult(Guid PaymentId, string PaymentUrl);
}
