namespace SalesManagement.Application.Features.Posts.CreatePost
{
    public record CreatePostCommand(
        Guid? Id,
        Guid ProfileRef,
        string? Title,
        string? Post,
        IReadOnlyList<(Stream Content, string? Extension)> Images,
        IReadOnlyList<Guid>? RemovePhotoIds);
}
