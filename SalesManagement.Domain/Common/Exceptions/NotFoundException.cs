namespace SalesManagement.Domain.Common.Exceptions
{
    /// <summary>زمانی که موجودیت درخواستی پیدا نمی‌شود. به 404 نگاشت می‌شود.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
