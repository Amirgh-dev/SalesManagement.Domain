namespace SalesManagement.Domain.Common.Exceptions
{
    /// <summary>
    /// زمانی که هویت کاربر تأیید نمی‌شود (اطلاعات ورود نادرست). به 401 نگاشت می‌شود.
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }
}
