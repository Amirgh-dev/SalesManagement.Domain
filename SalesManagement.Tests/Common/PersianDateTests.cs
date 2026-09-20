using SalesManagement.Domain.Common;

namespace SalesManagement.Tests.Common
{
    public class PersianDateTests
    {
        [Fact]
        public void Midnight_formats_date_only_with_slashes()
            => Assert.Equal("1405/06/29", PersianDate.Format(new DateTime(2026, 9, 20)));

        [Fact]
        public void With_time_includes_hour_and_minute()
            => Assert.Equal("1405/06/29 17:12", PersianDate.Format(new DateTime(2026, 9, 20, 17, 12, 19)));

        [Fact]
        public void Nowruz_boundaries()
        {
            Assert.Equal("1403/01/01", PersianDate.Format(new DateTime(2024, 3, 20)));
            Assert.Equal("1402/12/29", PersianDate.Format(new DateTime(2024, 3, 19)));
        }

        [Fact]
        public void DateOnly_and_DateTimeOffset_are_supported()
        {
            Assert.Equal("1405/07/09", PersianDate.Format(new DateOnly(2026, 10, 1)));
            Assert.Equal("1405/06/29 09:05", PersianDate.Format(new DateTimeOffset(2026, 9, 20, 9, 5, 0, TimeSpan.FromHours(3.5))));
        }

        [Fact]
        public void Out_of_range_returns_null()
            => Assert.Null(PersianDate.Format(DateTime.MinValue));

        [Fact]
        public void TryFormat_ignores_non_dates()
        {
            Assert.Null(PersianDate.TryFormat("2026-09-20"));
            Assert.Null(PersianDate.TryFormat(5));
            Assert.Null(PersianDate.TryFormat(null));
        }
    }
}
