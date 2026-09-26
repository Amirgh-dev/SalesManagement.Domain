using Dapper;
using SalesManagement.Application.Common.Interfaces;
using SalesManagement.Domain.Features.Users;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.Users.QueryUser
{
    public class UserQueryRepository(
        ISepidarConnectionFactory connectionFactory,
        ISepidarConfigurationProvider configProvider) : IUserQueryRepository
    {
        private const string SqlSummary = """
            DECLARE @FiscalYear int = (SELECT MAX(FiscalYearId) FROM FMK.FiscalYear);

            SELECT p.PartyId, p.Name, p.LastName,
                   ISNULL(t.TotalDebit, 0) AS TotalDebit,
                   ISNULL(t.TotalCredit, 0) AS TotalCredit
            FROM GNR.Party AS p
                OUTER APPLY (
                    SELECT SUM(vi.Debit) AS TotalDebit, SUM(vi.Credit) AS TotalCredit
                    FROM ACC.VoucherItem AS vi
                        INNER JOIN ACC.Voucher AS v ON v.VoucherId = vi.VoucherRef
                    WHERE vi.DLRef = p.DLRef AND v.FiscalYearRef = @FiscalYear AND vi.AccountSLRef = @DefaultSLRef
                ) AS t
            WHERE p.PartyId = @PartyId;
            """;

        public async Task<UserSummary?> GetSummaryAsync(int partyId, CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();
            var config = configProvider.Get();

            var row = await connection.QueryFirstOrDefaultAsync<SummaryRow>(new CommandDefinition(
                SqlSummary, new { PartyId = partyId, config.DefaultSLRef }, cancellationToken: cancellationToken));

            if (row is null) return null;

            var fullName = $"{row.Name} {row.LastName}".Trim();
            return new UserSummary(row.PartyId, fullName, row.TotalDebit, row.TotalCredit);
        }

        private sealed class SummaryRow
        {
            public int PartyId { get; set; }
            public string? Name { get; set; }
            public string? LastName { get; set; }
            public decimal TotalDebit { get; set; }
            public decimal TotalCredit { get; set; }
        }
    }
}
