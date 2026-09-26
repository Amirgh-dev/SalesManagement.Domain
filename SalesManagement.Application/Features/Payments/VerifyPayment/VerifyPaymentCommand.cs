namespace SalesManagement.Application.Features.Payments.VerifyPayment
{
    public record VerifyPaymentCommand(Guid PaymentId, string Status, string? Authority);
}
