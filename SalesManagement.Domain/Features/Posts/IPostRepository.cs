namespace SalesManagement.Domain.Features.Posts
{
    public interface IPostRepository
    {
        Task<IReadOnlyList<string>> CreateAsync(NewPost post, CancellationToken cancellationToken = default);
    }
}
