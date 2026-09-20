using System.Globalization;

namespace SalesManagement.Domain.Common
{
    public static class PersianDate
    {
        private static readonly PersianCalendar Calendar = new();
        private static readonly DateTime MinSupported = Calendar.MinSupportedDateTime;
        private static readonly DateTime MaxSupported = Calendar.MaxSupportedDateTime;

        public static string? Format(DateTime value)
        {
            if (value < MinSupported || value > MaxSupported) return null;

            var date = $"{Calendar.GetYear(value):0000}/{Calendar.GetMonth(value):00}/{Calendar.GetDayOfMonth(value):00}";

            return value.TimeOfDay == TimeSpan.Zero
                ? date
                : $"{date} {value.Hour:00}:{value.Minute:00}";
        }

        public static string? Format(DateOnly value) => Format(value.ToDateTime(TimeOnly.MinValue));

        public static string? Format(DateTimeOffset value) => Format(value.DateTime);

        public static string? TryFormat(object? value) => value switch
        {
            DateTime dt => Format(dt),
            DateOnly d => Format(d),
            DateTimeOffset dto => Format(dto),
            _ => null,
        };
    }
}
