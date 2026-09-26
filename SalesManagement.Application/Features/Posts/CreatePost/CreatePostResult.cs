namespace SalesManagement.Application.Features.Posts.CreatePost
{
    public record PostPhotoResult(Guid Id, string? Url);

    public record CreatePostResult(Guid Id, bool IsUpdate, IReadOnlyList<PostPhotoResult> Photos);
}
