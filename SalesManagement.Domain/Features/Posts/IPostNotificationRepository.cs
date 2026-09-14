namespace SalesManagement.Domain.Features.Posts
{
    public interface IPostNotificationRepository
    {
        Task NotifyProvinceOfUrgentPostAsync(Guid profileRef, CancellationToken cancellationToken = default);
    }
}
