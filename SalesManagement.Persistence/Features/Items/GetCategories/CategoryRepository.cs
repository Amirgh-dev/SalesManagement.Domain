using SalesManagement.Persistence.ConnectionFactories;
using Dapper;
using SalesManagement.Domain.Features.Items;
using SalesManagement.Persistence.Features.Items.GetCategories.Dtos;

namespace SalesManagement.Persistence.Features.Items.GetCategories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ISepidarConnectionFactory _connectionFactory;

        public CategoryRepository(ISepidarConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string Sql = """
            SELECT g.GroupingID AS GroupingId, g.Code, g.Title, g.ParentGroupRef, g.EntityType
            FROM GNR.Grouping AS g
            WHERE g.EntityType = @EntityType AND LEN(g.Code) IN (1, 2)
            ORDER BY g.Code;
            """;

        public async Task<IReadOnlyList<Category>> GetSaleGroupsAsync(
            string entityType, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var command = new CommandDefinition(
                Sql, new { EntityType = entityType }, cancellationToken: cancellationToken);

            var rows = await connection.QueryAsync<CategoryDto>(command);

            return rows
                .Select(d => Category.Reconstitute(d.GroupingId, d.Code, d.Title, d.ParentGroupRef, d.EntityType))
                .ToList();
        }
    }
}
