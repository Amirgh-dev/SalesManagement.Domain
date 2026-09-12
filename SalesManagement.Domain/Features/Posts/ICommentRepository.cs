namespace SalesManagement.Domain.Features.Posts
{
    public interface ICommentRepository
    {
        Task CreateAsync(NewComment comment, CancellationToken cancellationToken = default);
    }
}
