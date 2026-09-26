using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.Customers;

namespace SalesManagement.Tests.Features.Customers
{
    public class IranianIdentifiersTests
    {
        [Theory]
        [InlineData("0499370899")]
        [InlineData("۰۴۹۹۳۷۰۸۹۹")]
        [InlineData("٠٤٩٩٣٧٠٨٩٩")]
        [InlineData("  0499370899  ")]
        public void Valid_national_codes_are_normalized(string input)
            => Assert.Equal("0499370899", IranianIdentifiers.NormalizeNationalCode(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("049937089")]
        [InlineData("04993708999")]
        [InlineData("04993708ab")]
        [InlineData("0499370898")]
        [InlineData("0000000000")]
        [InlineData("1111111111")]
        [InlineData("1234567890")]
        public void Invalid_national_codes_throw(string? input)
            => Assert.Throws<DomainException>(() => IranianIdentifiers.NormalizeNationalCode(input));

        [Theory]
        [InlineData("09123456789", "09123456789")]
        [InlineData("0912 345 6789", "09123456789")]
        [InlineData("0912-345-6789", "09123456789")]
        [InlineData("+989123456789", "09123456789")]
        [InlineData("00989123456789", "09123456789")]
        [InlineData("989123456789", "09123456789")]
        [InlineData("9123456789", "09123456789")]
        [InlineData("۰۹۱۲۳۴۵۶۷۸۹", "09123456789")]
        public void Mobile_is_normalized_to_09_format(string input, string expected)
            => Assert.Equal(expected, IranianIdentifiers.NormalizeMobile(input));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("02112345678")]
        [InlineData("0912345678")]
        [InlineData("091234567890")]
        [InlineData("abc")]
        [InlineData("08123456789")]
        public void Invalid_mobiles_throw(string? input)
            => Assert.Throws<DomainException>(() => IranianIdentifiers.NormalizeMobile(input));

        [Fact]
        public void Optional_phone_only_trims_and_converts_digits()
        {
            Assert.Equal("", IranianIdentifiers.NormalizeOptionalPhone(null));
            Assert.Equal("02112345678", IranianIdentifiers.NormalizeOptionalPhone(" ۰۲۱۱۲۳۴۵۶۷۸ "));
        }
    }
}
