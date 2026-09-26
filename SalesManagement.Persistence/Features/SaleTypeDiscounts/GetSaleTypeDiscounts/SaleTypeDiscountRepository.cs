using Dapper;
using SalesManagement.Domain.Features.SaleTypeDiscounts;
using SalesManagement.Persistence.ConnectionFactories;

namespace SalesManagement.Persistence.Features.SaleTypeDiscounts.GetSaleTypeDiscounts
{
    public class SaleTypeDiscountRepository(ISepidyarhesabConnectionFactory connectionFactory)
        : ISaleTypeDiscountRepository
    {
        private const string SqlSelectAll = """
            SELECT Id, SaleTypeRef, Title, Discount, IsCash, BlackList
            FROM dbo.SaleTypeDiscount
            ORDER BY SaleTypeRef, Id;
            """;

        public async Task<IReadOnlyList<SaleTypeDiscount>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            using var connection = connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<SaleTypeDiscountRow>(
                new CommandDefinition(SqlSelectAll, cancellationToken: cancellationToken));

            return rows
                .Select(r => SaleTypeDiscount.Reconstitute(
                    r.Id, r.SaleTypeRef, r.Title, r.Discount, r.IsCash, r.BlackList))
                .ToList();
        }

        private sealed class SaleTypeDiscountRow
        {
            public int Id { get; set; }
            public int SaleTypeRef { get; set; }
            public string? Title { get; set; }
            public decimal? Discount { get; set; }
            public int? IsCash { get; set; }
            public bool? BlackList { get; set; }
        }
    }
}
