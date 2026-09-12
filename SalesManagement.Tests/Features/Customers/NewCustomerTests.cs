using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Tests.Features.Customers
{
    public class NewCustomerTests
    {
        private static readonly NewCustomerExtra ValidExtra = new(null, null, null, null, null, null, null);

        private static NewCustomer Make(
            string? firstName = "Ali",
            string? lastName = "Rezaei",
            string? nationalCode = "0012345678",
            string? landline = "02100000000",
            string? mobile = "09120000000",
            string? messenger = "09120000000",
            string? address = "Some Address",
            string? zipCode = "1234567890",
            int locationRef = 1,
            string? geolocation = null,
            NewCustomerExtra? extra = null)
            => NewCustomer.Create(
                firstName, lastName, nationalCode, landline, mobile, messenger,
                address, zipCode, locationRef, geolocation, extra ?? ValidExtra);

        // ---- required fields ------------------------------------------

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
        public void Blank_landline_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(landline: value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_messenger_throws(string? value)
        {
            Assert.Throws<DomainException>(() => Make(messenger: value));
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
                firstName: " Ali ", lastName: " Rezaei ", nationalCode: " 0012345678 ",
                landline: " 02100000000 ", mobile: " 09120000000 ", messenger: " 09120000000 ",
                address: " Some Address ", zipCode: " 1234567890 ");

            Assert.Equal("Ali", c.FirstName);
            Assert.Equal("Rezaei", c.LastName);
            Assert.Equal("0012345678", c.NationalCode);
            Assert.Equal("02100000000", c.LandlinePhone);
            Assert.Equal("09120000000", c.MobilePhone);
            Assert.Equal("09120000000", c.MessengerPhone);
            Assert.Equal("Some Address", c.Address);
            Assert.Equal("1234567890", c.ZipCode);
        }

        // ---- geolocation ----------------------------------------------

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

        // ---- files -----------------------------------------------------

        [Fact]
        public void Files_default_to_null()
        {
            var c = Make();
            Assert.Null(c.LicenseFileName);
            Assert.Null(c.ProfileFileName);
        }

        [Fact]
        public void WithLicenseFile_sets_license_file_name()
        {
            var c = Make().WithLicenseFile("license.jpg");
            Assert.Equal("license.jpg", c.LicenseFileName);
        }

        [Fact]
        public void WithProfileFile_sets_profile_file_name()
        {
            var c = Make().WithProfileFile("profile.jpg");
            Assert.Equal("profile.jpg", c.ProfileFileName);
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
