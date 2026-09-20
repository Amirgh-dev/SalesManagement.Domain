using System.Data;
using Dapper;
using SalesManagement.Domain.Common;

namespace SalesManagement.Persistence.Common
{
    internal static class DapperRows
    {
        private const string PersianSuffix = "Persian";

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
                .Select(MapRow)
                .ToList();

        private static IDictionary<string, object?> MapRow(IDictionary<string, object> row)
        {
            var result = new Dictionary<string, object?>(row.Count * 2);

            foreach (var (key, raw) in row)
                result[key] = raw is DBNull ? null : raw;

            foreach (var (key, value) in result.ToList())
            {
                var persian = PersianDate.TryFormat(value);
                var persianKey = key + PersianSuffix;
                if (persian is not null && !result.ContainsKey(persianKey))
                    result[persianKey] = persian;
            }

            return result;
        }
    }
}
