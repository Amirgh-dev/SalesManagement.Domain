namespace SalesManagement.Application.Features.Payments.RequestPayment
{
    public record RequestPaymentCommand(
        int PartyRef, string? TableName, int? TargetRef, long Amount, string Description,
        string Channel, string IpAddress);
}
