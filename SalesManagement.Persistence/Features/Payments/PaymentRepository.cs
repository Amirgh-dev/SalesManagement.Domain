using Dapper;
using SalesManagement.Domain.Enums;
using SalesManagement.Domain.Features.Payments;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Payments
{
    public class PaymentRepository(IZanisServicesConnectionFactory connectionFactory) : IPaymentRepository
    {
        private const string SqlInsert = """
            INSERT INTO dbo.Payments
                (Id, PartyRef, TableName, TargetRef, Gateway, Amount, Description, Authority, State, IsPay, IsOk,
                 Modifier, LastModificationDate, RefId, TrackingCode, CardPan, CardHash, Fee, ErrorCode, ErrorMessage,
                 Channel, IpAddress, CreationDate, VerifiedDate, LastModifiedDate)
            VALUES
                (@Id, @PartyRef, @TableName, @TargetRef, @Gateway, @Amount, @Description, @Authority, @State, @IsPay, @IsOk,
                 @Modifier, @LastModificationDate, @RefId, @TrackingCode, @CardPan, @CardHash, @Fee, @ErrorCode, @ErrorMessage,
                 @Channel, @IpAddress, @CreationDate, @VerifiedDate, @LastModifiedDate);
            """;

        private const string SqlUpdate = """
            UPDATE dbo.Payments
            SET Authority = @Authority, State = @State, IsPay = @IsPay, IsOk = @IsOk, Modifier = @Modifier,
                LastModificationDate = @LastModificationDate, RefId = @RefId, CardPan = @CardPan, CardHash = @CardHash,
                Fee = @Fee, ErrorCode = @ErrorCode, ErrorMessage = @ErrorMessage, VerifiedDate = @VerifiedDate,
                LastModifiedDate = @LastModifiedDate
            WHERE Id = @Id;
            """;

        private const string SqlSelectColumns = """
            SELECT Id, PartyRef, TableName, TargetRef, Gateway, Amount, Description, Authority, State, IsPay, IsOk,
                   Modifier, LastModificationDate, RefId, TrackingCode, CardPan, CardHash, Fee, ErrorCode, ErrorMessage,
                   Channel, IpAddress, CreationDate, VerifiedDate, LastModifiedDate
            FROM dbo.Payments
            """;

        private const string SqlExistsVerified = """
            SELECT TOP (1) 1 FROM dbo.Payments
            WHERE PartyRef = @PartyRef AND TableName = @TableName AND TargetRef = @TargetRef AND State = @State;
            """;

        private const string SqlSelectById = SqlSelectColumns + " WHERE Id = @Id;";

        private const string SqlSelectByAuthority = SqlSelectColumns + " WHERE Authority = @Authority;";

        public async Task InsertAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(SqlInsert, ToArgs(payment), cancellationToken: cancellationToken));
        }

        public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(SqlUpdate, ToArgs(payment), cancellationToken: cancellationToken));
        }

        public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(
                new CommandDefinition(SqlSelectById, new { Id = id }, cancellationToken: cancellationToken));
            return row?.ToDomain();
        }

        public async Task<Payment?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(
                new CommandDefinition(SqlSelectByAuthority, new { Authority = authority }, cancellationToken: cancellationToken));
            return row?.ToDomain();
        }

        public async Task<bool> ExistsVerifiedAsync(
            int partyRef, string tableName, int targetRef, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var found = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    SqlExistsVerified,
                    new { PartyRef = partyRef, TableName = tableName, TargetRef = targetRef, State = (byte)PaymentState.Verified },
                    cancellationToken: cancellationToken));
            return found is not null;
        }

        private static object ToArgs(Payment p) => new
        {
            p.Id,
            p.PartyRef,
            p.TableName,
            p.TargetRef,
            p.Gateway,
            p.Amount,
            p.Description,
            p.Authority,
            State = (byte)p.State,
            p.IsPay,
            p.IsOk,
            p.Modifier,
            p.LastModificationDate,
            p.RefId,
            p.TrackingCode,
            p.CardPan,
            p.CardHash,
            p.Fee,
            p.ErrorCode,
            p.ErrorMessage,
            p.Channel,
            p.IpAddress,
            p.CreationDate,
            p.VerifiedDate,
            p.LastModifiedDate,
        };

        private sealed class PaymentRow
        {
            public Guid Id { get; set; }
            public int? PartyRef { get; set; }
            public string? TableName { get; set; }
            public int? TargetRef { get; set; }
            public string Gateway { get; set; } = "";
            public long Amount { get; set; }
            public string Description { get; set; } = "";
            public string? Authority { get; set; }
            public byte State { get; set; }
            public bool IsPay { get; set; }
            public bool IsOk { get; set; }
            public int Modifier { get; set; }
            public DateTime LastModificationDate { get; set; }
            public long? RefId { get; set; }
            public string? TrackingCode { get; set; }
            public string? CardPan { get; set; }
            public string? CardHash { get; set; }
            public int? Fee { get; set; }
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }
            public string Channel { get; set; } = "";
            public string IpAddress { get; set; } = "";
            public DateTime CreationDate { get; set; }
            public DateTime? VerifiedDate { get; set; }
            public DateTime? LastModifiedDate { get; set; }

            public Payment ToDomain() => Payment.Reconstitute(
                Id, PartyRef ?? 0, TableName, TargetRef, Gateway, Amount, Description, Authority,
                (PaymentState)State, IsPay, IsOk, Modifier, LastModificationDate, RefId, TrackingCode,
                CardPan, CardHash, Fee, ErrorCode, ErrorMessage, Channel, IpAddress, CreationDate,
                VerifiedDate, LastModifiedDate);
        }
    }
}
