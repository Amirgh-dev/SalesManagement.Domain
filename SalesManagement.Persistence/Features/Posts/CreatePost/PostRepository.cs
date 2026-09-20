using SalesManagement.Persistence.ConnectionFactories;
using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Persistence.Features.Posts.CreatePost
{
    public class PostRepository(IZanisServicesConnectionFactory connectionFactory) : IPostRepository
    {
        private const string SqlProfileGuard = """
            SELECT TOP (1) IsBlackList
            FROM dbo.ProfileParty
            WHERE Id = @ProfileRef;
            """;

        private const string SqlInsertPostFile = """
            INSERT INTO dbo.PostFile (Id, ProfileRef, PostRef, FileName, FileExtensions, IsOk, IsMain, Tables)
            VALUES (@Id, @ProfileRef, @PostRef, @FileName, @FileExtensions, @IsOk, @IsMain, @Tables);
            """;

        private const string SqlInsertPost = """
            INSERT INTO dbo.PostProfile (Id, ProfileRef, Title, Post, Type, LikePost, CreationDate)
            VALUES (@Id, @ProfileRef, @Title, @Post, @Type, @LikePost, @CreationDate);
            """;

        private const string SqlSelectOwnedPost = """
            SELECT TOP (1) Id
            FROM dbo.PostProfile
            WHERE Id = @Id AND ProfileRef = @ProfileRef;
            """;

        private const string SqlUpdatePost = """
            UPDATE dbo.PostProfile
            SET Title = @Title, Post = @Post, Type = @Type
            WHERE Id = @Id AND ProfileRef = @ProfileRef;
            """;

        private const string SqlSelectExistingPhotos = """
            SELECT Id, FileName, IsMain FROM dbo.PostFile WHERE PostRef = @PostRef;
            """;

        private const string SqlDeletePostFiles = """
            DELETE FROM dbo.PostFile WHERE PostRef = @PostRef;
            """;

        public async Task<IReadOnlyList<string>> CreateAsync(
            NewPost post, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var guard = await connection.QueryFirstOrDefaultAsync<ProfileGuardRow>(
                    SqlProfileGuard, new { post.ProfileRef }, tx);

                if (guard is null)
                    throw new DomainException("پروفایل پیدا نشد");
                if (guard.IsBlackList == true)
                    throw new DomainException("این پروفایل مسدود است و نمی‌تواند پست اضافه کند");

                Guid postId;
                var deletedFileNames = new List<string>();

                if (post.Id is null)
                {
                    postId = Guid.NewGuid();
                    var pPost = new DynamicParameters();
                    pPost.Add("@Id", postId);
                    pPost.Add("@ProfileRef", post.ProfileRef);
                    pPost.Add("@Title", post.Title);
                    pPost.Add("@Post", post.Body);
                    pPost.Add("@Type", post.Type.GetDescription());
                    pPost.Add("@LikePost", 0L);
                    pPost.Add("@CreationDate", DateTime.Now);
                    await connection.ExecuteAsync(SqlInsertPost, pPost, tx);
                }
                else
                {
                    var existing = await connection.QueryFirstOrDefaultAsync<Guid?>(
                        SqlSelectOwnedPost, new { post.Id, post.ProfileRef }, tx);
                    if (existing is null)
                        throw new DomainException("پست پیدا نشد");
                    postId = existing.Value;

                    var pUpd = new DynamicParameters();
                    pUpd.Add("@Id", postId);
                    pUpd.Add("@ProfileRef", post.ProfileRef);
                    pUpd.Add("@Title", post.Title);
                    pUpd.Add("@Post", post.Body);
                    pUpd.Add("@Type", post.Type.GetDescription());
                    await connection.ExecuteAsync(SqlUpdatePost, pUpd, tx);

                    if (post.PhotoFileNames.Count > 0)
                    {
                        var existingPhotos = (await connection.QueryAsync<ExistingPhotoRow>(
                            SqlSelectExistingPhotos, new { PostRef = postId }, tx))
                            .ToList();

                        if (existingPhotos.Count > 0)
                        {
                            deletedFileNames.AddRange(existingPhotos.Select(f => f.FileName));
                            await connection.ExecuteAsync(SqlDeletePostFiles, new { PostRef = postId }, tx);
                        }
                    }
                }

                foreach (var fileName in post.PhotoFileNames)
                {
                    var ext = Path.GetExtension(fileName);
                    if (ext.Length > 5) ext = ext[..5];

                    var pFile = new DynamicParameters();
                    pFile.Add("@Id", Guid.NewGuid());
                    pFile.Add("@ProfileRef", post.ProfileRef);
                    pFile.Add("@PostRef", postId);
                    pFile.Add("@FileName", fileName);
                    pFile.Add("@FileExtensions", ext);
                    pFile.Add("@IsOk", true);
                    pFile.Add("@IsMain", false);
                    pFile.Add("@Tables", "PostFile");
                    await connection.ExecuteAsync(SqlInsertPostFile, pFile, tx);
                }

                await tx.CommitAsync(cancellationToken);
                return deletedFileNames;
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

        private sealed class ExistingPhotoRow
        {
            public Guid Id { get; set; }
            public string FileName { get; set; } = "";
            public bool? IsMain { get; set; }
        }
    }
}
