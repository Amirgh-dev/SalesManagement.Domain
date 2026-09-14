namespace SalesManagement.Application.Features.Notifications.GetNotifications
{
    public record GetNotificationsCommand(int PartyRef, int? Skip, int? Take);
}
