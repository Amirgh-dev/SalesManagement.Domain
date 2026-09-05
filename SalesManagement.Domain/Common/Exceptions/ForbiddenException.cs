namespace SalesManagement.Domain.Common.Exceptions
{
    /// <summary>
    /// زمانی که کاربر شناسایی شده اما اجازه‌ی انجام عمل را ندارد (مثلاً مسدود بودن). به 403 نگاشت می‌شود.
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }
}
