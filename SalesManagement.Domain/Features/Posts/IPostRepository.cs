namespace SalesManagement.Domain.Features.Posts
{
    public sealed record PostPhotoRef(Guid Id, string Path);

    public sealed record PostSaveOutcome(
        Guid PostId, IReadOnlyList<string> DeletedFileNames, IReadOnlyList<PostPhotoRef> Photos);

    public interface IPostRepository
    {
        Task<PostSaveOutcome> CreateAsync(NewPost post, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> DeleteAsync(
            Guid postId, Guid profileRef, CancellationToken cancellationToken = default);
    }
}
