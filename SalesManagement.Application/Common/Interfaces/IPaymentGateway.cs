namespace SalesManagement.Application.Common.Interfaces
{
    public sealed record GatewayRequestResult(
        bool Success, string? Authority, string? PaymentUrl, int? Fee, string? ErrorCode, string? ErrorMessage);

    public sealed record GatewayVerifyResult(
        bool Success, long? RefId, string? CardPan, string? CardHash, string? ErrorCode, string? ErrorMessage);

    public interface IPaymentGateway
    {
        string Name { get; }

        Task<GatewayRequestResult> RequestAsync(
            long amount, string description, string callbackUrl, CancellationToken cancellationToken = default);

        Task<GatewayVerifyResult> VerifyAsync(
            long amount, string authority, CancellationToken cancellationToken = default);
    }
}
