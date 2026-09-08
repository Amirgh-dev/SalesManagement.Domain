namespace SalesManagement.Domain.Features.Customers
{
    public interface ICustomerRepository
    {
        Task<int> CreateAsync(
            NewCustomer customer, string? visitorCode, CancellationToken cancellationToken = default);
    }
}