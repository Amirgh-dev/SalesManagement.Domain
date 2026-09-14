namespace SalesManagement.Domain.Features.Customers
{
    public interface ICustomerRepository
    {
        Task<int> CreateAsync(
            NewCustomer customer, string? visitorCode, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> UpdateAsync(
            NewCustomer customer, string? visitorCode, CancellationToken cancellationToken = default);
    }
}