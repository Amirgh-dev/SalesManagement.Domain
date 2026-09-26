using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Application.Features.Posts.DeleteComment
{
    public record DeleteCommentCommand(Guid? Id, Guid? ProfileRef);

    public class DeleteCommentHandler(
        ICommentRepository commentRepository,
        ILogger<DeleteCommentHandler> logger)
    {
        public async Task Handle(DeleteCommentCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.Id is null || cmd.Id == Guid.Empty) throw new DomainException("شناسه‌ی کامنت نامعتبر است");
            if (cmd.ProfileRef is null || cmd.ProfileRef == Guid.Empty) throw new DomainException("شناسه‌ی پروفایل نامعتبر است");

            await commentRepository.DeleteAsync(cmd.Id.Value, cmd.ProfileRef.Value, cancellationToken);

            logger.LogInformation("کامنت {CommentId} حذف شد", cmd.Id);
        }
    }
}
