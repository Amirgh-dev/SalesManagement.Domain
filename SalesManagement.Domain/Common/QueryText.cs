using System.Diagnostics.CodeAnalysis;

namespace SalesManagement.Domain.Common
{
    internal static class QueryText
    {
        public static bool IsBlankOrPlaceholder([NotNullWhen(false)] string? s) =>
            string.IsNullOrWhiteSpace(s) || string.Equals(s.Trim(), "string", StringComparison.OrdinalIgnoreCase);

        public static List<string> SplitNames(string? csv) => (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(n => !string.Equals(n, "string", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        public static FilterOp? ParseOp(string? op) => (op ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "eq" or "" => FilterOp.Eq,
            "ne" => FilterOp.Ne,
            "gt" => FilterOp.Gt,
            "gte" => FilterOp.Gte,
            "lt" => FilterOp.Lt,
            "lte" => FilterOp.Lte,
            "contains" => FilterOp.Contains,
            _ => null,
        };
    }
}
