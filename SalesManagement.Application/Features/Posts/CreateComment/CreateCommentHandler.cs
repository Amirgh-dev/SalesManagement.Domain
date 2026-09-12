using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Application.Features.Posts.CreateComment
{
    public class CreateCommentHandler(
        ICommentRepository commentRepository,
        ILogger<CreateCommentHandler> logger)
    {
        public async Task Handle(CreateCommentCommand cmd, CancellationToken cancellationToken = default)
        {
            var comment = NewComment.Create(cmd.Id, cmd.PostRef, cmd.ProfileRef, cmd.Comment);

            await commentRepository.CreateAsync(comment, cancellationToken);

            logger.LogInformation(
                "کامنتِ پست {PostRef} از پروفایل {ProfileRef} {Mode}",
                cmd.PostRef, cmd.ProfileRef, comment.IsUpdate ? "به‌روزرسانی شد" : "ثبت شد");
        }
    }
}
