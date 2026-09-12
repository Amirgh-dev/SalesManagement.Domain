namespace SalesManagement.Application.Features.Posts.CreateComment
{
    public record CreateCommentCommand(Guid? Id, Guid PostRef, Guid ProfileRef, string? Comment);
}
