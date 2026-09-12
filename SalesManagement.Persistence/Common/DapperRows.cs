using System.Data;
using Dapper;

namespace SalesManagement.Persistence.Common
{
    internal static class DapperRows
    {
        public static async Task<List<IDictionary<string, object?>>> ReadRowsAsync(
            IDbConnection connection, string sql, object? param, CancellationToken ct)
        {
            var command = new CommandDefinition(sql, param, cancellationToken: ct);
            return MapRows(await connection.QueryAsync(command));
        }

        public static async Task<(int Total, List<IDictionary<string, object?>> Rows)> ReadCountAndRowsAsync(
            IDbConnection connection, string sql, object? param, CancellationToken ct)
        {
            var command = new CommandDefinition(sql, param, cancellationToken: ct);
            using var multi = await connection.QueryMultipleAsync(command);

            var total = await multi.ReadFirstAsync<int>();
            var rows = MapRows(await multi.ReadAsync());
            return (total, rows);
        }

        public static List<IDictionary<string, object?>> MapRows(IEnumerable<dynamic> rows) =>
            rows
                .Cast<IDictionary<string, object>>()
                .Select(r => (IDictionary<string, object?>)r.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value is DBNull ? null : kv.Value))
                .ToList();
    }
}
