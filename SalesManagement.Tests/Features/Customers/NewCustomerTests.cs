using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Tests.Features.Customers
{
    public class NewCustomerTests
    {
        private static readonly NewCustomerExtra ValidExtra = new(null, null, null, null, null, null, null);

        private static NewCustomer Make(
            int? partyId = null,
            string? firstName = "Ali",
            string? lastName = "Rezaei",
            string? nationalCode = "0499370899",
            string? landline = "02100000000",
            string? mobile = "09120000000",
            string? messenger = "09120000000",
            string? address = "Some Address",
            string? zipCode = "1234567890",
            int locationRef = 1,
            string? geolocation = null,
            NewCustomerExtra? extra = null)
            => NewCustomer.Create(
                partyId, firstName, lastName, nationalCode, landline, mobile, messenger,
                address, zipCode, locationRef, geolocation, extra ?? ValidExtra);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_firstName_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(firstName: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_lastName_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(lastName: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_mobile_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(mobile: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_landline_is_allowed_and_becomes_empty(string? value)
        {
            Assert.Equal("", Make(landline: value).LandlinePhone);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_messenger_is_allowed_and_becomes_empty(string? value)
        {
            Assert.Equal("", Make(messenger: value).MessengerPhone);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_nationalCode_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(nationalCode: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_address_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(address: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_zipCode_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(zipCode: value));
        }

        [Fact]
        public void All_text_fields_are_trimmed()
        {
            var c = Make(
                firstName: " Ali ", lastName: " Rezaei ", nationalCode: " 0499370899 ",
                landline: " 02100000000 ", mobile: " 09120000000 ", messenger: " 09120000000 ",
                address: " Some Address ", zipCode: " 1234567890 ");

            Assert.Equal("Ali", c.FirstName);
            Assert.Equal("Rezaei", c.LastName);
            Assert.Equal("0499370899", c.NationalCode);
            Assert.Equal("02100000000", c.LandlinePhone);
            Assert.Equal("09120000000", c.MobilePhone);
            Assert.Equal("09120000000", c.MessengerPhone);
            Assert.Equal("Some Address", c.Address);
            Assert.Equal("1234567890", c.ZipCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_geolocation_defaults_to_zero_zero(string? geo)
        {
            var c = Make(geolocation: geo);
            Assert.Equal(0m, c.Latitude);
            Assert.Equal(0m, c.Longitude);
        }

        [Fact]
        public void Valid_geolocation_is_parsed()
        {
            var c = Make(geolocation: "35.7,51.4");
            Assert.Equal(35.7m, c.Latitude);
            Assert.Equal(51.4m, c.Longitude);
        }

        [Theory]
        [InlineData("35.7")]
        [InlineData("abc,51.4")]
        [InlineData("35.7,xyz")]
        public void Invalid_geolocation_throws(string geo)
        {
            Assert.Throws<DomainException>(() => Make(geolocation: geo));
        }

        [Fact]
        public void PartyId_null_means_create_and_IsUpdate_is_false()
        {
            var c = Make(partyId: null);
            Assert.Null(c.PartyId);
            Assert.False(c.IsUpdate);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PartyId_zero_or_negative_is_treated_as_create(int partyId)
        {
            var c = Make(partyId: partyId);
            Assert.Null(c.PartyId);
            Assert.False(c.IsUpdate);
        }

        [Fact]
        public void PartyId_positive_means_update()
        {
            var c = Make(partyId: 42);
            Assert.Equal(42, c.PartyId);
            Assert.True(c.IsUpdate);
        }

        [Fact]
        public void Validation_still_applies_when_updating()
        {
            Assert.Throws<DomainException>(() => Make(partyId: 42, firstName: "  "));
        }

        [Fact]
        public void WithLicenseFile_replaces_existing_file_name_on_update()
        {
            var c = Make(partyId: 42).WithLicenseFile("old.jpg").WithLicenseFile("new.jpg");
            Assert.Equal("new.jpg", c.LicenseFileName);
        }

        [Fact]
        public void Files_default_to_null()
        {
            var c = Make();
            Assert.Null(c.LicenseFileName);
        }

        [Fact]
        public void WithLicenseFile_sets_license_file_name()
        {
            var c = Make().WithLicenseFile("license.jpg");
            Assert.Equal("license.jpg", c.LicenseFileName);
        }

        [Fact]
        public void Extra_is_preserved()
        {
            var extra = new NewCustomerExtra(
                DateTime.Today, true, "history", "operational history", null, null, "My Store");
            var c = Make(extra: extra);
            Assert.Same(extra, c.Extra);
        }
    }
}
