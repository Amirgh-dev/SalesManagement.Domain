using System.Globalization;
using SalesManagement.Domain.Common.Exceptions;

namespace SalesManagement.Domain.Features.PartyProfiles
{
    public sealed record PartyProfile
    {
        public int PartyRef { get; }
        public string UserName { get; }
        public string? AboutMe { get; }
        public string? Address { get; }
        public decimal? Latitude { get; }
        public decimal? Longitude { get; }
        public string? ProfilePhotoFileName { get; private init; }
        public IReadOnlyList<string> PhotoFileNames { get; private init; } = [];

        private PartyProfile(
            int partyRef, string userName, string? aboutMe, string? address,
            decimal? latitude, decimal? longitude)
        {
            PartyRef = partyRef;
            UserName = userName;
            AboutMe = aboutMe;
            Address = address;
            Latitude = latitude;
            Longitude = longitude;
        }

        public static PartyProfile Create(
            int partyRef, string? userName, string? aboutMe, string? address, string? geolocation)
        {
            if (partyRef <= 0) throw new DomainException("شناسه‌ی کاربر نامعتبر است");
            if (string.IsNullOrWhiteSpace(userName)) throw new DomainException("نام کاربری را وارد کنید");

            var (lat, lng) = ParseGeolocation(geolocation);

            return new PartyProfile(
                partyRef,
                userName.Trim(),
                string.IsNullOrWhiteSpace(aboutMe) ? null : aboutMe.Trim(),
                string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
                lat, lng);
        }

        public PartyProfile WithProfilePhoto(string? fileName) => this with { ProfilePhotoFileName = fileName };

        public PartyProfile WithPhotos(IReadOnlyList<string> fileNames) => this with { PhotoFileNames = fileNames };

        private static (decimal? Lat, decimal? Lng) ParseGeolocation(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return (null, null);

            var parts = value.Trim().Split(',');
            if (parts.Length != 2
                || !decimal.TryParse(parts[0].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lat)
                || !decimal.TryParse(parts[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var lng))
                throw new DomainException("لوکیشن نامعتبر است (فرمت درست: lat,lng)");

            return (lat, lng);
        }
    }
}
