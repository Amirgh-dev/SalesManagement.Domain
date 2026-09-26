namespace SalesManagement.Domain.Features.Payments
{
    public interface IPaymentRepository
    {
        Task InsertAsync(Payment payment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
        Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default);
        Task<bool> ExistsVerifiedAsync(
            int partyRef, string tableName, int targetRef, CancellationToken cancellationToken = default);
    }
}
