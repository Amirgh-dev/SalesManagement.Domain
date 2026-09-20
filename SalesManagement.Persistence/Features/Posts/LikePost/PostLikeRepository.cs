using SalesManagement.Persistence.ConnectionFactories;
using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Persistence.Features.Posts.LikePost
{
    public class PostLikeRepository(IZanisServicesConnectionFactory connectionFactory) : IPostLikeRepository
    {
        private const string SqlLikePost = """
            UPDATE dbo.PostProfile
            SET LikePost = LikePost + 1
            OUTPUT INSERTED.LikePost
            WHERE Id = @Id;
            """;

        public async Task<long> LikeAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var newLikeCount = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(SqlLikePost, new { Id = postId }, cancellationToken: cancellationToken));

            if (newLikeCount is null)
                throw new DomainException("پست پیدا نشد");

            return newLikeCount.Value;
        }
    }
}
