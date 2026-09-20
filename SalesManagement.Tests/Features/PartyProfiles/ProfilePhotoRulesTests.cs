using SalesManagement.Domain.Common.Exceptions;
using SalesManagement.Domain.Features.PartyProfiles;

namespace SalesManagement.Tests.Features.PartyProfiles
{
    public class ProfilePhotoRulesTests
    {
        [Theory]
        [InlineData(".jpg", 100)]
        [InlineData("PNG", 100)]
        [InlineData(".webp", 5L * 1024 * 1024)]
        public void Accepts_valid(string ext, long len) => ProfilePhotoRules.Validate(ext, len);

        [Theory]
        [InlineData(".mp4", 100)]
        [InlineData(".exe", 100)]
        [InlineData("", 100)]
        [InlineData(".jpg", 0)]
        [InlineData(".jpg", 5L * 1024 * 1024 + 1)]
        public void Rejects_invalid(string ext, long len)
            => Assert.Throws<DomainException>(() => ProfilePhotoRules.Validate(ext, len));
    }
}
