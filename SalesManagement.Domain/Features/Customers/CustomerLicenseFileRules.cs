using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Customers
{
    public static class CustomerLicenseFileRules
    {
        public const long MaxBytes = 5L * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"];

        public static string NormalizeExtension(string? extension)
        {
            var ext = (extension ?? "").Trim().ToLowerInvariant();
            if (ext.Length > 0 && !ext.StartsWith('.')) ext = "." + ext;
            return ext;
        }

        public static void Validate(string? extension, long length)
        {
            if (length <= 0)
                throw new DomainException("فایل مدرک خالی است");
            if (!AllowedExtensions.Contains(NormalizeExtension(extension)))
                throw new DomainException("فرمت فایل مدرک مجاز نیست (فقط jpg, jpeg, png, webp, gif, pdf)");
            if (length > MaxBytes)
                throw new DomainException("حجم فایل مدرک نباید بیشتر از ۵ مگابایت باشد");
        }
    }
}
