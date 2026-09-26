using SalesManagement.Domain.Common;

namespace SalesManagement.Tests.Common
{
    public class MediaUrlTests
    {
        [Theory]
        [InlineData("https://api.zanislight.ir", "/media/images/Profile/a.jpg", "https://api.zanislight.ir/media/images/Profile/a.jpg")]
        [InlineData("https://api.zanislight.ir/", "/media/images/Profile/a.jpg", "https://api.zanislight.ir/media/images/Profile/a.jpg")]
        [InlineData("https://api.zanislight.ir", "media/images/Profile/a.jpg", "https://api.zanislight.ir/media/images/Profile/a.jpg")]
        [InlineData("  https://api.zanislight.ir  ", " /media/x.png ", "https://api.zanislight.ir/media/x.png")]
        public void Combines_base_and_path_with_single_slash(string baseUrl, string path, string expected)
            => Assert.Equal(expected, MediaUrl.Combine(baseUrl, path));

        [Theory]
        [InlineData("", "/media/x.png", "/media/x.png")]
        [InlineData(null, "media/x.png", "/media/x.png")]
        public void Empty_base_returns_relative_path(string? baseUrl, string path, string expected)
            => Assert.Equal(expected, MediaUrl.Combine(baseUrl, path));

        [Theory]
        [InlineData("https://api.zanislight.ir", null)]
        [InlineData("https://api.zanislight.ir", "")]
        [InlineData("https://api.zanislight.ir", "  ")]
        public void No_path_returns_null(string baseUrl, string? path)
            => Assert.Null(MediaUrl.Combine(baseUrl, path));
    }
}
