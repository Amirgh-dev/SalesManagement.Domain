namespace SalesManagement.Application.Common.Interfaces
{
    public interface IPaymentCallbackUrlProvider
    {
        string Build(Guid paymentId);
    }
}
