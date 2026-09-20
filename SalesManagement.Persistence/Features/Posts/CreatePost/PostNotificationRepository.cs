using SalesManagement.Persistence.ConnectionFactories;
using System.Data.Common;
using Dapper;
using SalesManagement.Domain.Features.Posts;

namespace SalesManagement.Persistence.Features.Posts.CreatePost
{
    public class PostNotificationRepository(
        IZanisServicesConnectionFactory zanisServicesFactory,
        ISepidyarhesabConnectionFactory hesabFactory)
        : IPostNotificationRepository
    {
        public const string BroadcastCode = "SP-1002";

        private const string SqlSelectProfile = """
            SELECT TOP (1) p.UserName, p.PartyRef, addr.LocationRef
            FROM dbo.ProfileParty AS p
                OUTER APPLY (
                    SELECT TOP (1) a.LocationRef
                    FROM [Sepidar02].[GNR].[PartyAddress] AS a
                    WHERE a.PartyRef = p.PartyRef AND a.IsMain = 1
                ) AS addr
            WHERE p.Id = @ProfileRef;
            """;

        private const string SqlInsertBroadcastNotification = """
            INSERT INTO dbo.Notification
                (Id, Code, UserRef, PartyRef, VendorRef, QuotationRef, ItemRef, Note, CreateDate, IsSeen, IsHome)
            VALUES
                (NEWID(), @Code, @UserRef, @PartyRef, NULL, NULL, NULL, @Note, @Now, 0, 1);
            """;

        public async Task NotifyProvinceOfUrgentPostAsync(
            Guid profileRef, CancellationToken cancellationToken = default)
        {
            await using var zanisServices = (DbConnection)zanisServicesFactory.CreateConnection();
            await zanisServices.OpenAsync(cancellationToken);

            var profile = await zanisServices.QueryFirstOrDefaultAsync<ProfileRow>(
                new CommandDefinition(SqlSelectProfile, new { ProfileRef = profileRef }, cancellationToken: cancellationToken));

            // no address on file for this profile -> no province to broadcast into, nothing to write
            if (profile?.LocationRef is null)
                return;

            var note = $"توسط کاربر {profile.UserName} یک پست فوری کی داره در استان شما منتشر کرد";

            await using var hesab = (DbConnection)hesabFactory.CreateConnection();
            await hesab.OpenAsync(cancellationToken);

            await hesab.ExecuteAsync(new CommandDefinition(
                SqlInsertBroadcastNotification,
                new
                {
                    Code = BroadcastCode,
                    UserRef = profileRef,
                    PartyRef = profile.PartyRef,
                    Note = note,
                    Now = DateTime.Now,
                },
                cancellationToken: cancellationToken));
        }

        private sealed class ProfileRow
        {
            public string UserName { get; set; } = "";
            public int PartyRef { get; set; }
            public int? LocationRef { get; set; }
        }
    }
}
