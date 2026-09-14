namespace SalesManagement.Domain.Features.Notifications
{
    public interface INotificationQueryRepository
    {
        Task<(int Total, IReadOnlyList<IDictionary<string, object?>> Notifications)> GetForPartyAsync(
            int partyRef, int skip, int take, CancellationToken cancellationToken = default);
    }
}
