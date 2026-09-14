namespace SalesManagement.Application.Features.Notifications.GetNotifications
{
    public record GetNotificationsResult(int Total, IReadOnlyList<IDictionary<string, object?>> Notifications);
}
