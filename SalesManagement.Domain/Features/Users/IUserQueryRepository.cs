namespace SalesManagement.Domain.Features.Users
{
    public interface IUserQueryRepository
    {
        Task<UserSummary?> GetSummaryAsync(int partyId, CancellationToken cancellationToken = default);
    }
}
