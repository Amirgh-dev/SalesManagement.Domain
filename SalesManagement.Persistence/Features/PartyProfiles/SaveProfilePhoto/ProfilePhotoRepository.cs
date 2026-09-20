using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.PartyProfiles;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.PartyProfiles.SaveProfilePhoto
{
    public class ProfilePhotoRepository(IZanisServicesConnectionFactory connectionFactory) : IProfilePhotoRepository
    {
        private const string SqlSelectProfile = "SELECT TOP (1) Id FROM dbo.ProfileParty WHERE PartyRef = @PartyRef;";

        private const string SqlSelectMainPhotos =
            "SELECT Id, FileName FROM dbo.PartyFile WHERE ProfileRef = @ProfileRef AND IsMain = 1;";

        private const string SqlDeletePartyFile = "DELETE FROM dbo.PartyFile WHERE Id = @Id;";

        private const string SqlInsertPartyFile = """
            INSERT INTO dbo.PartyFile (Id, PartyRef, ProfileRef, FileName, FileExtensions, IsOk, IsMain, Tables)
            VALUES (@Id, @PartyRef, @ProfileRef, @FileName, @FileExtensions, 1, 1, 'ProfileParty');
            """;

        public async Task<string?> ReplaceMainPhotoAsync(
            int partyRef, string fileName, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var profileId = await connection.QueryFirstOrDefaultAsync<Guid?>(
                    SqlSelectProfile, new { PartyRef = partyRef }, tx);
                if (profileId is null)
                    throw new DomainException("ابتدا پروفایل خود را بسازید");

                var existing = (await connection.QueryAsync<ExistingPhotoRow>(
                    SqlSelectMainPhotos, new { ProfileRef = profileId }, tx)).ToList();

                foreach (var photo in existing)
                    await connection.ExecuteAsync(SqlDeletePartyFile, new { photo.Id }, tx);

                var ext = Path.GetExtension(fileName);
                if (ext.Length > 5) ext = ext[..5];

                await connection.ExecuteAsync(SqlInsertPartyFile, new
                {
                    Id = Guid.NewGuid(),
                    PartyRef = partyRef,
                    ProfileRef = profileId,
                    FileName = fileName,
                    FileExtensions = ext,
                }, tx);

                await tx.CommitAsync(cancellationToken);
                return existing.Select(p => p.FileName).FirstOrDefault();
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private sealed class ExistingPhotoRow
        {
            public Guid Id { get; set; }
            public string FileName { get; set; } = "";
        }
    }
}
