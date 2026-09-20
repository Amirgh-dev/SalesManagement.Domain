namespace SalesManagement.Domain.Features.Posts
{
    public interface IPostLikeRepository
    {
        Task<long> LikeAsync(Guid postId, CancellationToken cancellationToken = default);
    }
}
