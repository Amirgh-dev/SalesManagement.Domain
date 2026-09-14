using SalesManagement.Domain.Enums;

namespace SalesManagement.Application.Features.Posts.CreatePost
{
    public record CreatePostCommand(
        Guid? Id,
        Guid ProfileRef,
        string? Title,
        string? Post,
        TypePost? Type,
        IReadOnlyList<(Stream Content, string? Extension)> Images);
}
