using System.Globalization;
using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Enums;

namespace SalesManagement.Domain.Features.Customers
{
    public sealed record NewCustomer
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string NationalCode { get; }
        public string LandlinePhone { get; }
        public string MobilePhone { get; }
        public string MessengerPhone { get; }
        public string Address { get; }
        public string ZipCode { get; }
        public int LocationRef { get; }
        public decimal Latitude { get; }
        public decimal Longitude { get; }
        public NewCustomerExtra Extra { get; }
        public string? LicenseFileName { get; private init; }
        public string? ProfileFileName { get; private init; }

        private NewCustomer(string firstName, string lastName, string nationalCode,
            string landline, string mobile, string messenger, string address, string zipCode,
            int locationRef, decimal latitude, decimal longitude, NewCustomerExtra extra)
        {
            FirstName = firstName;
            LastName = lastName;
            NationalCode = nationalCode;
            LandlinePhone = landline;
            MobilePhone = mobile;
            MessengerPhone = messenger;
            Address = address;
            ZipCode = zipCode;
            LocationRef = locationRef;
            Latitude = latitude;
            Longitude = longitude;
            Extra = extra;
        }

        public static NewCustomer Create(
            string? firstName, string? lastName, string? nationalCode,
            string? landline, string? mobile, string? messenger,
            string? address, string? zipCode, int locationRef, string? geolocation,
            NewCustomerExtra extra)
        {
            if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("نام مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("نام خانوادگی مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(mobile)) throw new DomainException("شماره موبایل مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(landline)) throw new DomainException("تلفن ثابت مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(messenger)) throw new DomainException("شماره پیام‌رسان مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(nationalCode)) throw new DomainException("کد ملی مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(address)) throw new DomainException("آدرس مشتری را وارد کنید");
            if (string.IsNullOrWhiteSpace(zipCode)) throw new DomainException("کد پستی مشتری را وارد کنید");

            var (lat, lng) = ParseGeolocation(geolocation);

            return new NewCustomer(
                firstName.Trim(), lastName.Trim(), nationalCode.Trim(),
                landline.Trim(), mobile.Trim(), messenger.Trim(),
                address.Trim(), zipCode.Trim(), locationRef, lat, lng, extra);
        }

        public NewCustomer WithLicenseFile(string? fileName) => this with { LicenseFileName = fileName };

        public NewCustomer WithProfileFile(string? fileName) => this with { ProfileFileName = fileName };

        private static (decimal Lat, decimal Lng) ParseGeolocation(string? value)
        {
            var raw = string.IsNullOrWhiteSpace(value) ? "0,0" : value.Trim();
            var parts = raw.Split(',');
            if (parts.Length != 2
                || !decimal.TryParse(parts[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lat)
                || !decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lng))
                throw new DomainException("لوکیشن مشتری نامعتبر است (فرمت درست: lat,lng)");
            return (lat, lng);
        }
    }

    public sealed record NewCustomerExtra(
        DateTime? DateBirth,
        bool? Gender,
        string? HistoryElectricalActivity,
        string? ShopOperationalHistory,
        TypeOwnership? TypeOwnership,
        AreaSpecialization? AreaSpecialization,
        string? StoreName);
}
