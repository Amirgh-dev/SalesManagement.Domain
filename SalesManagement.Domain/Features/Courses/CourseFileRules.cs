using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.Courses
{
    public sealed record NewCourseFile(string FileName, string Extension, bool IsMain);

    public static class CourseFileRules
    {
        public const long MaxImageBytes = 5L * 1024 * 1024;
        public const long MaxVideoBytes = 50L * 1024 * 1024;

        private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
        private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm"];

        public static string NormalizeExtension(string? extension)
        {
            var ext = (extension ?? "").Trim().ToLowerInvariant();
            if (ext.Length > 0 && !ext.StartsWith('.')) ext = "." + ext;
            return ext;
        }

        public static void Validate(string? extension, long length, bool isBanner)
        {
            var ext = NormalizeExtension(extension);

            if (length <= 0)
                throw new DomainException("فایل خالی است");

            if (ImageExtensions.Contains(ext))
            {
                if (length > MaxImageBytes)
                    throw new DomainException("حجم هر عکس نباید بیشتر از ۵ مگابایت باشد");
                return;
            }

            if (VideoExtensions.Contains(ext))
            {
                if (isBanner)
                    throw new DomainException("بنر دوره باید عکس باشد");
                if (length > MaxVideoBytes)
                    throw new DomainException("حجم هر فیلم نباید بیشتر از ۵۰ مگابایت باشد");
                return;
            }

            throw new DomainException("فرمت فایل مجاز نیست (فقط jpg, jpeg, png, webp, gif, mp4, webm)");
        }
    }
}
