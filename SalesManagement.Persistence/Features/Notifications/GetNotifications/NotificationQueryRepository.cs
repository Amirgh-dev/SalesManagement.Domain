using SalesManagement.Persistence.ConnectionFactories;
using SalesManagement.Persistence.Features.Posts.CreatePost;
using System.Data.Common;
using Dapper;
using static SalesManagement.Persistence.Common.DapperRows;
using SalesManagement.Domain.Features.Notifications;

namespace SalesManagement.Persistence.Features.Notifications.GetNotifications
{
    public class NotificationQueryRepository(ISepidyarhesabConnectionFactory connectionFactory)
        : INotificationQueryRepository
    {
        private const int MaxAgeHours = 48;

        private const string SqlResolvePartyContext = """
            SELECT TOP (1)
                p.IsBroker AS [IsBroker],
                CASE WHEN l.Type = '2' THEN l.LocationId ELSE pr.LocationId END AS [ProvinceId]
            FROM [Sepidar02].[GNR].[Party] AS p
                LEFT JOIN [Sepidar02].[GNR].[PartyAddress] AS a ON a.PartyRef = p.PartyId AND a.IsMain = 1
                LEFT JOIN [Sepidar02].[GNR].[Location] AS l ON l.LocationId = a.LocationRef
                LEFT JOIN [Sepidar02].[GNR].[Location] AS pr ON pr.LocationId = l.Parent AND pr.Type = 2
            WHERE p.PartyId = @PartyRef;
            """;

        private const string SqlWhere = """
            WHERE n.CreateDate >= @CutoffDate
                AND (
                    (n.Code <> @BroadcastCode AND n.PartyRef = @PartyRef)
                    OR
                    (n.Code = @BroadcastCode AND n.PartyRef <> @PartyRef
                        AND @IsBroker = 0 AND @ProvinceId IS NOT NULL
                        AND EXISTS (
                            SELECT 1
                            FROM [Sepidar02].[GNR].[PartyAddress] AS pa
                            WHERE pa.PartyRef = n.PartyRef AND pa.IsMain = 1
                                AND (pa.LocationRef = @ProvinceId
                                     OR pa.LocationRef IN (
                                         SELECT LocationId FROM [Sepidar02].[GNR].[Location] WHERE Parent = @ProvinceId
                                     ))
                        ))
                )
            """;

        public async Task<(int Total, IReadOnlyList<IDictionary<string, object?>> Notifications)> GetForPartyAsync(
            int partyRef, int skip, int take, CancellationToken cancellationToken = default)
        {
            await using var connection = (DbConnection)connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var context = await connection.QueryFirstOrDefaultAsync<PartyContextRow>(
                new CommandDefinition(
                    SqlResolvePartyContext, new { PartyRef = partyRef }, cancellationToken: cancellationToken));

            var p = new DynamicParameters();
            p.Add("@PartyRef", partyRef);
            p.Add("@BroadcastCode", PostNotificationRepository.BroadcastCode);
            p.Add("@ProvinceId", context?.ProvinceId);
            p.Add("@IsBroker", context?.IsBroker ?? false);
            p.Add("@CutoffDate", DateTime.Now.AddHours(-MaxAgeHours));
            p.Add("@Skip", skip);
            p.Add("@Take", take);

            var sql = $"""
                SELECT COUNT(*)
                FROM dbo.Notification AS n
                {SqlWhere};

                SELECT n.Id AS [id], n.Code AS [code], n.UserRef AS [userRef], n.PartyRef AS [partyRef],
                       n.VendorRef AS [vendorRef], n.QuotationRef AS [quotationRef], n.ItemRef AS [itemRef],
                       n.Note AS [note], n.CreateDate AS [createDate], n.IsSeen AS [isSeen], n.IsHome AS [isHome]
                FROM dbo.Notification AS n
                {SqlWhere}
                ORDER BY n.CreateDate DESC
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
                """;

            var (total, rows) = await ReadCountAndRowsAsync(connection, sql, p, cancellationToken);
            return (total, rows);
        }

        private sealed class PartyContextRow
        {
            public bool IsBroker { get; set; }
            public int? ProvinceId { get; set; }
        }
    }
}
