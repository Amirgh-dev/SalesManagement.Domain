using System.Text;
using System.Text.RegularExpressions;
using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Customers
{
    public static partial class IranianIdentifiers
    {
        public static string ToLatinDigits(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                sb.Append(ch switch
                {
                    >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                    >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                    _ => ch,
                });
            }
            return sb.ToString();
        }

        public static string NormalizeNationalCode(string? value)
        {
            var code = ToLatinDigits((value ?? "").Trim());

            if (code.Length != 10 || !code.All(char.IsAsciiDigit))
                throw new DomainException("کد ملی باید ۱۰ رقم و فقط عدد باشد");

            if (code.Distinct().Count() == 1 || !HasValidCheckDigit(code))
                throw new DomainException("کد ملی نامعتبر است");

            return code;
        }

        public static string NormalizeMobile(string? value)
        {
            var raw = ToLatinDigits((value ?? "").Trim());
            var digits = NonPhoneChars().Replace(raw, "");

            if (digits.StartsWith("+98")) digits = "0" + digits[3..];
            else if (digits.StartsWith("0098")) digits = "0" + digits[4..];
            else if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
            else if (digits.Length == 10 && digits[0] == '9') digits = "0" + digits;

            if (!MobilePattern().IsMatch(digits))
                throw new DomainException("شماره موبایل نامعتبر است (مثال: 09123456789)");

            return digits;
        }

        public static string NormalizeOptionalPhone(string? value) =>
            ToLatinDigits((value ?? "").Trim());

        private static bool HasValidCheckDigit(string code)
        {
            var sum = 0;
            for (var i = 0; i < 9; i++)
                sum += (code[i] - '0') * (10 - i);

            var remainder = sum % 11;
            var check = remainder < 2 ? remainder : 11 - remainder;
            return check == code[9] - '0';
        }

        [GeneratedRegex(@"[\s\-\(\)\.]")]
        private static partial Regex NonPhoneChars();

        [GeneratedRegex(@"^09\d{9}$")]
        private static partial Regex MobilePattern();
    }
}
