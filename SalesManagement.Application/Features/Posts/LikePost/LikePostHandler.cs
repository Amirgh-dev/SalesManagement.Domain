using Microsoft.Extensions.Logging;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Application.Features.Posts.LikePost
{
    public class LikePostHandler(
        IPostLikeRepository postLikeRepository,
        ILogger<LikePostHandler> logger)
    {
        public async Task<LikePostResult> Handle(
            LikePostCommand cmd, CancellationToken cancellationToken = default)
        {
            if (cmd.PostId == Guid.Empty)
                throw new DomainException("شناسه‌ی پست نامعتبر است");

            var newLikeCount = await postLikeRepository.LikeAsync(cmd.PostId, cancellationToken);

            logger.LogInformation(
                "پست {PostId} لایک شد، تعداد لایک فعلی={LikeCount}", cmd.PostId, newLikeCount);

            return new LikePostResult(newLikeCount);
        }
    }
}
