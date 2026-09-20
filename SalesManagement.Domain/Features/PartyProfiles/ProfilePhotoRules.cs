using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.PartyProfiles
{
    public static class ProfilePhotoRules
    {
        public const long MaxBytes = 5L * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

        public static string NormalizeExtension(string? extension)
        {
            var ext = (extension ?? "").Trim().ToLowerInvariant();
            if (ext.Length > 0 && !ext.StartsWith('.')) ext = "." + ext;
            return ext;
        }

        public static void Validate(string? extension, long length)
        {
            if (length <= 0)
                throw new DomainException("فایل خالی است");
            if (!AllowedExtensions.Contains(NormalizeExtension(extension)))
                throw new DomainException("فرمت عکس مجاز نیست (فقط jpg, jpeg, png, webp, gif)");
            if (length > MaxBytes)
                throw new DomainException("حجم عکس نباید بیشتر از ۵ مگابایت باشد");
        }
    }
}
