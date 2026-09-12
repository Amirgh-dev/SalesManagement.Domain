namespace SalesManagement.Persistence.Common
{
    internal static class PersianText
    {
        public static string NormalizeFa(string s) => s
            .Replace('ي', 'ی')
            .Replace('ك', 'ک')
            .Replace("‌", string.Empty);

        public static string FaNormExpr(string textExpr) =>
            $"REPLACE(REPLACE(REPLACE({textExpr}, NCHAR(0x064A), NCHAR(0x06CC)), NCHAR(0x0643), NCHAR(0x06A9)), NCHAR(0x200C), N'')";
    }
}
