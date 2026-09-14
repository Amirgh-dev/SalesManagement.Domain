using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.Notifications;

namespace SalesManagement.Application.Features.Notifications.GetNotifications
{
    public class GetNotificationsHandler(
        INotificationQueryRepository notificationQueryRepository,
        ILogger<GetNotificationsHandler> logger)
    {
        private const int DefaultTake = 50;
        private const int MaxTake = 200;

        public async Task<GetNotificationsResult> Handle(
            GetNotificationsCommand cmd, CancellationToken cancellationToken = default)
        {
            var take = cmd.Take is null or <= 0 ? DefaultTake : Math.Min(cmd.Take.Value, MaxTake);
            var skip = cmd.Skip is null or < 0 ? 0 : cmd.Skip.Value;

            var (total, notifications) = await notificationQueryRepository.GetForPartyAsync(
                cmd.PartyRef, skip, take, cancellationToken);

            logger.LogInformation(
                "کوئری نوتیفیکیشن برای پارتی {PartyRef}: total={Total}", cmd.PartyRef, total);

            return new GetNotificationsResult(total, notifications);
        }
    }
}
