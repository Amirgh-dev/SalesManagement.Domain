namespace SalesManagement.Domain.Enums
{
    public enum PaymentState : byte
    {
        Created = 0,
        SentToGateway = 1,
        RequestFailed = 2,
        CanceledByUser = 3,
        Verified = 4,
        VerifyFailed = 5,
    }
}
