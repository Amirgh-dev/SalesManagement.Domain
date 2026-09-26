using Microsoft.Extensions.Logging;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Application.Features.Posts.DeletePost
{
    public record DeletePostCommand(Guid? Id, Guid? ProfileRef);

    public class DeletePostHandler(
        IPostRepository postRepository,
        IFileStorage fileStorage,
        ILogger<DeletePostHandler> logger)
    {
        private const string PostFileFolder = "PostFile";

        public async Task Handle(DeletePostCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.Id is null || cmd.Id == Guid.Empty) throw new DomainException("شناسه‌ی پست نامعتبر است");
            if (cmd.ProfileRef is null || cmd.ProfileRef == Guid.Empty) throw new DomainException("شناسه‌ی پروفایل نامعتبر است");

            var deletedFileNames = await postRepository.DeleteAsync(cmd.Id.Value, cmd.ProfileRef.Value, cancellationToken);

            foreach (var fileName in deletedFileNames)
                await fileStorage.DeleteAsync(PostFileFolder, fileName, CancellationToken.None);

            logger.LogInformation("پست {PostId} حذف شد", cmd.Id);
        }
    }
}
