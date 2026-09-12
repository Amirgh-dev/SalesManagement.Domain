using System.Globalization;
using SalesManagement.Domain.Common;
using SalesManagement.Domain.Common.Exceptions;
using static SalesManagement.Persistence.Common.PersianText;

namespace SalesManagement.Persistence.Common
{
    internal enum Kind { Int, Decimal, DateTime, String, Bool }

    internal static class SqlFilter
    {
        public static string TextExpr(string expr, Kind kind) =>
            kind == Kind.DateTime
                ? $"CONVERT(nvarchar(30), {expr}, 120)"
                : $"CAST({expr} AS nvarchar(200))";

        public static string LikeExpr(string expr, Kind kind) =>
            $"{FaNormExpr(TextExpr(expr, kind))} LIKE @s";

        public static (string Sql, object? Value) Condition(
            string expr, Kind kind, FilterOp op, string raw, string pName)
        {
            if (op == FilterOp.Contains)
                return ($"{FaNormExpr(TextExpr(expr, kind))} LIKE {pName}", "%" + NormalizeFa(raw) + "%");

            var sqlOp = op switch
            {
                FilterOp.Eq => "=",
                FilterOp.Ne => "<>",
                FilterOp.Gt => ">",
                FilterOp.Gte => ">=",
                FilterOp.Lt => "<",
                FilterOp.Lte => "<=",
                _ => throw new DomainException("op پشتیبانی نمی‌شود"),
            };

            object? typed = kind switch
            {
                Kind.Int => int.TryParse(raw, out var i)
                    ? i : throw new DomainException($"مقدار عددی نامعتبر: {raw}"),
                Kind.Decimal => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                    ? d : throw new DomainException($"مقدار عددی نامعتبر: {raw}"),
                Kind.DateTime => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt : throw new DomainException($"تاریخ نامعتبر: {raw}"),
                Kind.Bool => ParseBool(raw) ? 1 : 0,
                _ => raw,
            };

            return ($"{expr} {sqlOp} {pName}", typed);
        }

        private static bool ParseBool(string raw) => raw.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => throw new DomainException($"مقدار بولی نامعتبر: {raw}"),
        };
    }
}
