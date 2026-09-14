using SalesManagement.Persistence.ConnectionFactories;
using System.Data.Common;
using Dapper;
using static SalesManagement.Persistence.Common.SepidarDb;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Persistence.Features.PartyProfiles.SavePartyProfile
{
    public class PartyProfileRepository(
        ISepidyarhesabConnectionFactory connectionFactory,
        ISepidarConnectionFactory sepidarFactory)
        : IPartyProfileRepository
    {
        private const string SqlSelect = """
            SELECT TOP (1) Id
            FROM dbo.ProfileParty
            WHERE PartyRef = @PartyRef;
            """;

        private const string SqlInsert = """
            INSERT INTO dbo.ProfileParty
                (Id, PartyRef, UserName, AboutMe, IsBlackList, Rate)
            VALUES
                (@Id, @PartyRef, @UserName, @AboutMe, @IsBlackList, @Rate);
            """;

        private const string SqlUpdate = """
            UPDATE dbo.ProfileParty
            SET UserName = @UserName, AboutMe = @AboutMe
            WHERE Id = @Id;
            """;

        private const string SqlSelectMainPhoto = """
            SELECT TOP (1) Id, FileName FROM dbo.PartyFile WHERE ProfileRef = @ProfileRef AND IsMain = 1;
            """;

        private const string SqlSelectGalleryPhotos = """
            SELECT Id, FileName FROM dbo.PartyFile WHERE ProfileRef = @ProfileRef AND IsMain = 0;
            """;

        private const string SqlInsertPartyFile = """
            INSERT INTO dbo.PartyFile (Id, PartyRef, ProfileRef, FileName, FileExtensions, IsOk, IsMain, Tables)
            VALUES (@Id, @PartyRef, @ProfileRef, @FileName, @FileExtensions, @IsOk, @IsMain, @Tables);
            """;

        private const string SqlDeletePartyFile = """
            DELETE FROM dbo.PartyFile WHERE Id = @Id;
            """;

        private const string SqlDeleteGalleryPhotos = """
            DELETE FROM dbo.PartyFile WHERE ProfileRef = @ProfileRef AND IsMain = 0;
            """;

        private const string SqlSelectMainAddress = """
            SELECT TOP (1) PartyAddressId FROM GNR.PartyAddress WHERE PartyRef = @PartyRef AND IsMain = 1;
            """;

        private const string SqlInsertAddress = """
            INSERT INTO GNR.PartyAddress
                (PartyAddressId, PartyRef, IsMain, Type, Address, LocationRef, ZipCode, Adress_En, Version, Latitude, Longitude, Title)
            VALUES
                (@PartyAddressId, @PartyRef, 1, 2, @Address, NULL, NULL, @Address, 1, @Latitude, @Longitude, N'آدرس ثبت‌شده از طریق پروفایل');
            """;

        private const string SqlUpdateAddress = """
            UPDATE GNR.PartyAddress
            SET Address = COALESCE(@Address, Address), Adress_En = COALESCE(@Address, Adress_En),
                Latitude = COALESCE(@Latitude, Latitude), Longitude = COALESCE(@Longitude, Longitude)
            WHERE PartyAddressId = @PartyAddressId;
            """;

        public async Task<(bool Created, IReadOnlyList<string> DeletedFileNames)> UpsertAsync(
            PartyProfile profile, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await using var sepidar = (DbConnection)sepidarFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await sepidar.OpenAsync(cancellationToken);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            await using var txSepidar = await sepidar.BeginTransactionAsync(cancellationToken);

            try
            {
                var existingId = await connection.ExecuteScalarAsync<Guid?>(
                    SqlSelect, new { profile.PartyRef }, tx);

                var p = new DynamicParameters();
                p.Add("@PartyRef", profile.PartyRef);
                p.Add("@UserName", profile.UserName);
                p.Add("@AboutMe", profile.AboutMe);

                bool created;
                Guid profileId;
                if (existingId is null)
                {
                    profileId = Guid.NewGuid();
                    p.Add("@Id", profileId);
                    p.Add("@IsBlackList", false);
                    p.Add("@Rate", 5);
                    await connection.ExecuteAsync(SqlInsert, p, tx);
                    created = true;
                }
                else
                {
                    profileId = existingId.Value;
                    p.Add("@Id", profileId);
                    await connection.ExecuteAsync(SqlUpdate, p, tx);
                    created = false;
                }

                if (profile.Address is not null || profile.Latitude is not null || profile.Longitude is not null)
                {
                    var existingAddressId = await sepidar.ExecuteScalarAsync<int?>(
                        SqlSelectMainAddress, new { profile.PartyRef }, txSepidar);

                    if (existingAddressId is not null)
                    {
                        await sepidar.ExecuteAsync(
                            SqlUpdateAddress,
                            new
                            {
                                PartyAddressId = existingAddressId.Value,
                                profile.Address,
                                profile.Latitude,
                                profile.Longitude,
                            },
                            txSepidar);
                    }
                    else
                    {
                        var addressId = await NextIdAsync(sepidar, txSepidar, "GNR.PartyAddress");
                        await sepidar.ExecuteAsync(
                            SqlInsertAddress,
                            new
                            {
                                PartyAddressId = addressId,
                                profile.PartyRef,
                                Address = profile.Address ?? "",
                                profile.Latitude,
                                profile.Longitude,
                            },
                            txSepidar);
                    }
                }

                var deletedFileNames = new List<string>();

                if (profile.ProfilePhotoFileName is not null)
                {
                    var existingMain = await connection.QueryFirstOrDefaultAsync<ExistingPhotoRow>(
                        SqlSelectMainPhoto, new { ProfileRef = profileId }, tx);

                    if (existingMain is not null)
                    {
                        deletedFileNames.Add(existingMain.FileName);
                        await connection.ExecuteAsync(SqlDeletePartyFile, new { existingMain.Id }, tx);
                    }

                    await InsertPartyFileAsync(
                        connection, tx, profile.PartyRef, profileId,
                        profile.ProfilePhotoFileName, isMain: true);
                }

                if (profile.PhotoFileNames.Count > 0)
                {
                    var existingGallery = (await connection.QueryAsync<ExistingPhotoRow>(
                        SqlSelectGalleryPhotos, new { ProfileRef = profileId }, tx)).ToList();

                    if (existingGallery.Count > 0)
                    {
                        deletedFileNames.AddRange(existingGallery.Select(f => f.FileName));
                        await connection.ExecuteAsync(SqlDeleteGalleryPhotos, new { ProfileRef = profileId }, tx);
                    }

                    foreach (var fileName in profile.PhotoFileNames)
                        await InsertPartyFileAsync(connection, tx, profile.PartyRef, profileId, fileName, isMain: false);
                }

                await tx.CommitAsync(cancellationToken);
                await txSepidar.CommitAsync(cancellationToken);
                return (created, deletedFileNames);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                await txSepidar.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task InsertPartyFileAsync(
            DbConnection connection, DbTransaction tx, int partyRef, Guid profileId, string fileName, bool isMain)
        {
            var ext = Path.GetExtension(fileName);
            if (ext.Length > 5) ext = ext[..5];

            var p = new DynamicParameters();
            p.Add("@Id", Guid.NewGuid());
            p.Add("@PartyRef", partyRef);
            p.Add("@ProfileRef", profileId);
            p.Add("@FileName", fileName);
            p.Add("@FileExtensions", ext);
            p.Add("@IsOk", true);
            p.Add("@IsMain", isMain);
            p.Add("@Tables", "ProfileParty");
            await connection.ExecuteAsync(SqlInsertPartyFile, p, tx);
        }

        private sealed class ExistingPhotoRow
        {
            public Guid Id { get; set; }
            public string FileName { get; set; } = "";
        }
    }
}
