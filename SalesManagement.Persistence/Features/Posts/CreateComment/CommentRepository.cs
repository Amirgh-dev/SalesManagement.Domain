using SalesManagement.Persistence.ConnectionFactories;
using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Persistence.Features.Posts.CreateComment
{
    public class CommentRepository(IZanisServicesConnectionFactory connectionFactory) : ICommentRepository
    {
        private const string SqlProfileGuard = """
            SELECT TOP (1) IsBlackList
            FROM dbo.ProfileParty
            WHERE Id = @ProfileRef;
            """;

        private const string SqlPostExists = "SELECT TOP (1) 1 FROM dbo.PostProfile WHERE Id = @PostRef;";

        private const string SqlInsertComment = """
            INSERT INTO dbo.PartyComment (Id, PostRef, ProfileRef, Comment, CreationDate)
            VALUES (@Id, @PostRef, @ProfileRef, @Comment, @CreationDate);
            """;

        private const string SqlSelectOwnedComment = """
            SELECT TOP (1) Id FROM dbo.PartyComment WHERE Id = @Id AND ProfileRef = @ProfileRef;
            """;

        private const string SqlUpdateComment = """
            UPDATE dbo.PartyComment SET Comment = @Comment WHERE Id = @Id AND ProfileRef = @ProfileRef;
            """;

        public async Task CreateAsync(NewComment comment, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var guard = await connection.QueryFirstOrDefaultAsync<ProfileGuardRow>(
                    SqlProfileGuard, new { comment.ProfileRef }, tx);

                if (guard is null)
                    throw new DomainException("پروفایل پیدا نشد");
                if (guard.IsBlackList == true)
                    throw new DomainException("این پروفایل مسدود است و نمی‌تواند کامنت بگذارد");

                if (comment.Id is null)
                {
                    var postExists = await connection.ExecuteScalarAsync<int?>(
                        SqlPostExists, new { comment.PostRef }, tx);
                    if (postExists is null)
                        throw new DomainException("پست پیدا نشد");

                    var p = new DynamicParameters();
                    p.Add("@Id", Guid.NewGuid());
                    p.Add("@PostRef", comment.PostRef);
                    p.Add("@ProfileRef", comment.ProfileRef);
                    p.Add("@Comment", comment.Text);
                    p.Add("@CreationDate", DateTime.Now);
                    await connection.ExecuteAsync(SqlInsertComment, p, tx);
                }
                else
                {
                    var existing = await connection.QueryFirstOrDefaultAsync<Guid?>(
                        SqlSelectOwnedComment, new { comment.Id, comment.ProfileRef }, tx);
                    if (existing is null)
                        throw new DomainException("کامنت پیدا نشد");

                    await connection.ExecuteAsync(
                        SqlUpdateComment, new { comment.Id, comment.ProfileRef, Comment = comment.Text }, tx);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private sealed class ProfileGuardRow
        {
            public bool? IsBlackList { get; set; }
        }
    }
}
